#include "pch.h"

#include "common.h"
#include "com_object_factory.h"
#include "notifications.h"

#include <unknwn.h>
#include <winrt/base.h>
#include <winrt/Windows.Foundation.h>
#include <winrt/Windows.Data.Xml.Dom.h>
#include <winrt/Windows.Foundation.Collections.h>
#include <winrt/Windows.UI.Notifications.h>
#include <winrt/Windows.ApplicationModel.Background.h>

#include "winstore.h"

#include <winerror.h>
#include <NotificationActivationCallback.h>

#include "notifications_winrt/handler_functions.h"

using namespace winrt::Windows::ApplicationModel::Background;
using winrt::Windows::Data::Xml::Dom::XmlDocument;
using winrt::Windows::UI::Notifications::ToastNotification;
using winrt::Windows::UI::Notifications::ToastNotificationManager;

namespace
{
    constexpr std::wstring_view TASK_NAME = L"PowerToysBackgroundNotificationsHandler";
    constexpr std::wstring_view TASK_ENTRYPOINT = L"PowerToysNotifications.BackgroundHandler";
    constexpr std::wstring_view APPLICATION_ID = L"PowerToys";

    constexpr std::wstring_view WIN32_AUMID = L"Microsoft.PowerToysWin32";
}

namespace localized_strings 
{
    constexpr std::wstring_view SNOOZE_BUTTON = L"Snooze";

    constexpr std::wstring_view PT_UPDATE = L"PowerToys update";
    constexpr std::wstring_view DOWNLOAD_IN_PROGRESS = L"Downloading...";
    constexpr std::wstring_view DOWNLOAD_COMPLETE = L"Download complete";
}

static DWORD loop_thread_id()
{
    static const DWORD thread_id = GetCurrentThreadId();
    return thread_id;
}

class DECLSPEC_UUID("DD5CACDA-7C2E-4997-A62A-04A597B58F76") NotificationActivator : public INotificationActivationCallback
{
public:
    HRESULT __stdcall QueryInterface(_In_ REFIID iid, _Outptr_ void** resultInterface) override
    {
        static const QITAB qit[] = {
            QITABENT(NotificationActivator, INotificationActivationCallback),
            { 0 }
        };
        return QISearch(this, qit, iid, resultInterface);
    }

    ULONG __stdcall AddRef() override
    {
        return ++_refCount;
    }

    ULONG __stdcall Release() override
    {
        LONG refCount = --_refCount;
        if (refCount == 0)
        {
            PostThreadMessage(loop_thread_id(), WM_QUIT, 0, 0);
            delete this;
        }
        return refCount;
    }

    virtual HRESULT STDMETHODCALLTYPE Activate(
        LPCWSTR appUserModelId,
        LPCWSTR invokedArgs,
        const NOTIFICATION_USER_INPUT_DATA*,
        ULONG) override
    {
        auto lib = LoadLibraryW(L"Notifications.dll");
        if (!lib)
        {
            return 1;
        }
        auto dispatcher = reinterpret_cast<decltype(dispatch_to_background_handler)*>(GetProcAddress(lib, "dispatch_to_background_handler"));
        if (!dispatcher)
        {
            return 1;
        }

        dispatcher(invokedArgs);

        return 0;
    }

private:
    std::atomic<long> _refCount;
};

void notifications::run_desktop_app_activator_loop()
{
    com_object_factory<NotificationActivator> factory;

    (void)loop_thread_id();

    DWORD token;
    auto res = CoRegisterClassObject(__uuidof(NotificationActivator), &factory, CLSCTX_LOCAL_SERVER, REGCLS_MULTIPLEUSE, &token);
    if (!SUCCEEDED(res))
    {
        return;
    }

    run_message_loop();
    CoRevokeClassObject(token);
}

void notifications::register_background_toast_handler()
{
    if (!winstore::running_as_packaged())
    {
        // The WIX installer will have us registered via the registry
        return;
    }
    try
    {
        // Re-request access to clean up from previous PowerToys installations
        BackgroundExecutionManager::RemoveAccess();
        BackgroundExecutionManager::RequestAccessAsync().get();

        BackgroundTaskBuilder builder;
        ToastNotificationActionTrigger trigger{ APPLICATION_ID };
        builder.SetTrigger(trigger);
        builder.TaskEntryPoint(TASK_ENTRYPOINT);
        builder.Name(TASK_NAME);

        const auto tasks = BackgroundTaskRegistration::AllTasks();
        const bool already_registered = std::any_of(begin(tasks), end(tasks), [=](const auto& task) {
            return task.Value().Name() == TASK_NAME;
        });
        if (already_registered)
        {
            return;
        }
        (void)builder.Register();
    }
    catch (...)
    {
        // Couldn't register the background task, nothing we can do
    }
}

void notifications::show_toast(std::wstring message, toast_params params)
{
    // The toast won't be actually activated in the background, since it doesn't have any buttons
    show_toast_with_activations(std::move(message), {}, {}, std::move(params));
}

inline void xml_escape(std::wstring data)
{
    std::wstring buffer;
    buffer.reserve(data.size());
    for (size_t pos = 0; pos != data.size(); ++pos)
    {
        switch (data[pos])
        {
        case L'&':
            buffer.append(L"&amp;");
            break;
        case L'\"':
            buffer.append(L"&quot;");
            break;
        case L'\'':
            buffer.append(L"&apos;");
            break;
        case L'<':
            buffer.append(L"&lt;");
            break;
        case L'>':
            buffer.append(L"&gt;");
            break;
        default:
            buffer.append(&data[pos], 1);
            break;
        }
    }
    data.swap(buffer);
}

void notifications::show_toast_with_activations(std::wstring message, std::wstring_view background_handler_id, std::vector<action_t> actions, toast_params params)
{
    // DO NOT LOCALIZE any string in this function, because they're XML tags and a subject to
    // https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/toast-xml-schema

    std::wstring toast_xml;
    toast_xml.reserve(2048);
    std::wstring title{ L"PowerToys" };
    if (winstore::running_as_packaged())
    {
        title += L" (Experimental)";
    }

    toast_xml += LR"(<?xml version="1.0"?><toast><visual><binding template="ToastGeneric"><text>)";
    toast_xml += title;
    toast_xml += L"</text><text>";
    toast_xml += message;
    toast_xml += L"</text>";
    if (params.progress)
    {
        toast_xml += LR"(<progress title=")";
        toast_xml += localized_strings::PT_UPDATE;
        if (params.subtitle)
        {
            toast_xml += L" ";
            toast_xml += *params.subtitle;
        }
        toast_xml += LR"(" value="{progressValue}" valueStringOverride="{progressValueString}" status="{progressStatus}"/>)";
    }
    toast_xml += L"</binding></visual><actions>";
    for (size_t i = 0; i < size(actions); ++i)
    {
        std::visit(overloaded{
                       [&](const snooze_button& b) {
                           const bool has_durations = !b.durations.empty() && size(b.durations) <= 5;
                           std::wstring selection_id = L"snoozeTime";
                           selection_id += static_cast<wchar_t>(L'0' + i);
                           if (has_durations)
                           {
                               toast_xml += LR"(<input id=")";
                               toast_xml += selection_id;
                               toast_xml += LR"(" type="selection" defaultInput=")";
                               toast_xml += std::to_wstring(b.durations[0].minutes);
                               toast_xml += L'"';
                               if (!b.snooze_title.empty())
                               {
                                   toast_xml += LR"( title=")";
                                   toast_xml += b.snooze_title;
                                   toast_xml += L'"';
                               }
                               toast_xml += L'>';
                               for (const auto& duration : b.durations)
                               {
                                   toast_xml += LR"(<selection id=")";
                                   toast_xml += std::to_wstring(duration.minutes);
                                   toast_xml += LR"(" content=")";
                                   toast_xml += duration.label;
                                   toast_xml += LR"("/>)";
                               }
                               toast_xml += LR"(</input>)";
                           }
                       },
                       [](const auto&) {} },
                   actions[i]);
    }

    for (size_t i = 0; i < size(actions); ++i)
    {
        std::visit(overloaded{
                       [&](const link_button& b) {
                           toast_xml += LR"(<action activationType="protocol" )";
                           if (b.context_menu)
                           {
                               toast_xml += LR"(placement="contextMenu" )";
                           }
                           toast_xml += LR"(arguments=")";
                           toast_xml += b.url;
                           toast_xml += LR"(" content=")";
                           toast_xml += b.label;
                           toast_xml += LR"(" />)";
                       },
                       [&](const background_activated_button& b) {
                           toast_xml += LR"(<action activationType="background" )";
                           if (b.context_menu)
                           {
                               toast_xml += LR"(placement="contextMenu" )";
                           }
                           toast_xml += LR"(arguments=")";
                           toast_xml += L"button_id=" + std::to_wstring(i); // pass the button ID
                           toast_xml += L"&amp;handler=";
                           toast_xml += background_handler_id;
                           toast_xml += LR"(" content=")";
                           toast_xml += b.label;
                           toast_xml += LR"(" />)";
                       },
                       [&](const snooze_button& b) {
                           const bool has_durations = !b.durations.empty() && size(b.durations) <= 5;
                           std::wstring selection_id = L"snoozeTime";
                           selection_id += static_cast<wchar_t>(L'0' + i);
                           toast_xml += LR"(<action activationType="system" arguments="snooze" )";
                           if (has_durations)
                           {
                               toast_xml += LR"(hint-inputId=")";
                               toast_xml += selection_id;
                               toast_xml += '"';
                           }
                           toast_xml += LR"( content=")";
                           toast_xml += localized_strings::SNOOZE_BUTTON;
                           toast_xml += LR"(" />)";
                       } },
                   actions[i]);
    }
    toast_xml += L"</actions></toast>";

    XmlDocument toast_xml_doc;
    xml_escape(toast_xml);
    toast_xml_doc.LoadXml(toast_xml);
    ToastNotification notification{ toast_xml_doc };

    if (params.progress)
    {
        float progress = std::clamp(params.progress.value(), 0.0f, 1.0f);
        winrt::Windows::Foundation::Collections::StringMap map;
        map.Insert(L"progressValue", std::to_wstring(progress));
        map.Insert(L"progressValueString", std::to_wstring(static_cast<int>(progress * 100)) + std::wstring(L"%"));
        map.Insert(L"progressStatus", localized_strings::DOWNLOAD_IN_PROGRESS);
        winrt::Windows::UI::Notifications::NotificationData data(map);
        notification.Data(data);
    }

    const auto notifier = winstore::running_as_packaged() ? ToastNotificationManager::ToastNotificationManager::CreateToastNotifier() :
                                                            ToastNotificationManager::ToastNotificationManager::CreateToastNotifier(WIN32_AUMID);

    // Set a tag-related params if it has a valid length
    if (params.tag.has_value() && params.tag->length() < 64)
    {
        notification.Tag(*params.tag);
        if (!params.resend_if_scheduled)
        {
            for (const auto& scheduled_toast : notifier.GetScheduledToastNotifications())
            {
                if (scheduled_toast.Tag() == *params.tag)
                {
                    return;
                }
            }
        }
    }

    notifier.Show(notification);
}

void notifications::update_progress_bar_toast(std::wstring plaintext_message, toast_params params)
{
    if (!params.progress.has_value())
    {
        return;
    }

    const auto notifier = winstore::running_as_packaged() ? ToastNotificationManager::ToastNotificationManager::CreateToastNotifier() :
                                                            ToastNotificationManager::ToastNotificationManager::CreateToastNotifier(WIN32_AUMID);
    
    float progress = std::clamp(params.progress.value(), 0.0f, 1.0f);
    winrt::Windows::Foundation::Collections::StringMap map;
    map.Insert(L"progressValue", std::to_wstring(progress));
    map.Insert(L"progressValueString", std::to_wstring(static_cast<int>(progress * 100)) + std::wstring(L"%"));
    map.Insert(L"progressStatus", progress < 1 ? localized_strings::DOWNLOAD_IN_PROGRESS : localized_strings::DOWNLOAD_COMPLETE);

    
    winrt::Windows::UI::Notifications::NotificationData data(map);
    std::wstring tag = L"";
    if (params.tag.has_value() && params.tag->length() < 64)
    {
        tag = *params.tag;
    }

    winrt::Windows::UI::Notifications::NotificationUpdateResult res = notifier.Update(data, tag);
}
