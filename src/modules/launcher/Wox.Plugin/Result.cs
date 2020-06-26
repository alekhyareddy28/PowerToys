using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace Wox.Plugin
{

    public class Result
    {

        private string _pluginDirectory;
        private string _icoPath;
        public string Title { get; set; }
        public string SubTitle { get; set; }

        public string Glyph { get; set; }

        public string FontFamily { get; set; }

        /// <summary>
        /// The text that will get displayed in the Search text box, when this item is selected in the result list.
        /// </summary>
        public string QueryTextDisplay { get; set; }

        public string IcoPath
        {
            get { return _icoPath; }
            set
            {
                if (!string.IsNullOrEmpty(PluginDirectory) && !Path.IsPathRooted(value))
                {
                    _icoPath = Path.Combine(value, IcoPath);
                }
                else
                {
                    _icoPath = value;
                }
            }
        }

        public delegate ImageSource IconDelegate();

        public IconDelegate Icon;


        /// <summary>
        /// return true to hide wox after select result
        /// </summary>
        public Func<ActionContext, bool> Action { get; set; }

        public class Score
        {
            // For the folder, calculator and shell plugins which have a deterministic score
            public Score(int firstValue)
            {
                score = new List<int> { firstValue };
            }

            // For UWP applications
            public Score(int firstValue, int secondValue)
            {
                score = new List<int> { firstValue, secondValue };
            }

            // For Win32 applications
            public Score(int firstValue, int secondValue, int thirdValue)
            {
                score = new List<int> { firstValue, secondValue, thirdValue };
            }

            // To make it generic for any number of items so that it can be expanded in the future
            public Score(List<int> scores)
            {
                score = new List<int>(scores);
            }

            private List<int> score;

            public static bool operator <(Score firstResult, Score secondResult)
            {
                List<int> firstScore = firstResult.score;
                List<int> secondScore = secondResult.score;

                int count1 = firstScore.Count;
                int count2 = secondScore.Count;

                int numberOfItems = Math.Min(count1, count2);

                for (int index = 0; index < numberOfItems; index++)
                {
                    if (firstScore[index] < secondScore[index])
                    {
                        return true;
                    }
                }

                return false;
            }

            public static bool operator >(Score firstResult, Score secondResult)
            {
                List<int> firstScore = firstResult.score;
                List<int> secondScore = secondResult.score;

                int count1 = firstScore.Count;
                int count2 = secondScore.Count;

                int numberOfItems = Math.Min(count1, count2);

                for (int index = 0; index < numberOfItems; index++)
                {
                    if (firstScore[index] > secondScore[index])
                    {
                        return true;
                    }
                }

                return false;
            }

            public static Score operator +(Score firstResult, int increment)
            {
                if(firstResult == null)
                {
                    return null;
                }

                List<int> firstScore = firstResult.score;

                int count = firstScore.Count;

                for (int index = 0; index < count; index++)
                {
                    firstScore[index] += increment;
                }

                return new Result.Score(firstScore);
            }

        }

        public Score score { get; set; }

        /// <summary>
        /// A list of indexes for the characters to be highlighted in Title
        /// </summary>
        public IList<int> TitleHighlightData { get; set; }

        /// <summary>
        /// A list of indexes for the characters to be highlighted in SubTitle
        /// </summary>
        public IList<int> SubTitleHighlightData { get; set; }

        /// <summary>
        /// Only results that originQuery match with current query will be displayed in the panel
        /// </summary>
        internal Query OriginQuery { get; set; }

        /// <summary>
        /// Plugin directory
        /// </summary>
        public string PluginDirectory
        {
            get { return _pluginDirectory; }
            set
            {
                _pluginDirectory = value;
                if (!string.IsNullOrEmpty(IcoPath) && !Path.IsPathRooted(IcoPath))
                {
                    IcoPath = Path.Combine(value, IcoPath);
                }
            }
        }

        public override bool Equals(object obj)
        {
            var r = obj as Result;

            var equality = string.Equals(r?.Title, Title) &&
                           string.Equals(r?.SubTitle, SubTitle) &&
                           string.Equals(r?.IcoPath, IcoPath) &&
                           TitleHighlightData == r.TitleHighlightData &&
                           SubTitleHighlightData == r.SubTitleHighlightData;

            return equality;
        }

        public override int GetHashCode()
        {
            var hashcode = (Title?.GetHashCode() ?? 0) ^
                           (SubTitle?.GetHashCode() ?? 0);
            return hashcode;
        }

        public override string ToString()
        {
            return Title + SubTitle;
        }

        [Obsolete("Use IContextMenu instead")]
        /// <summary>
        /// Context menus associate with this result
        /// </summary>
        public List<Result> ContextMenu { get; set; }

        [Obsolete("Use Object initializers instead")]
        public Result(string Title, string IcoPath, string SubTitle = null)
        {
            this.Title = Title;
            this.IcoPath = IcoPath;
            this.SubTitle = SubTitle;
        }

        public Result() { }

        /// <summary>
        /// Additional data associate with this result
        /// </summary>
        public object ContextData { get; set; }

        /// <summary>
        /// Plugin ID that generated this result
        /// </summary>
        public string PluginID { get; internal set; }

        

        /*public static bool operator<(Result firstResult, Result secondResult)
        {
            List<int> firstScore = firstResult.Score;
            List<int> secondScore = secondResult.Score;

            int count1 = firstScore.Count;
            int count2 = secondScore.Count;

            int numberOfItems = Math.Min(count1, count2);

            for(int index = 0; index < numberOfItems; index++)
            {
                if(firstScore[index] < secondScore[index])
                {
                    return true;
                }
            }

            return false;
        }

        public static bool operator >(Result firstResult, Result secondResult)
        {
            List<int> firstScore = firstResult.Score;
            List<int> secondScore = secondResult.Score;

            int count1 = firstScore.Count;
            int count2 = secondScore.Count;

            int numberOfItems = Math.Min(count1, count2);

            for (int index = 0; index < numberOfItems; index++)
            {
                if (firstScore[index] > secondScore[index])
                {
                    return true;
                }
            }

            return false;
        }*/
    }
}