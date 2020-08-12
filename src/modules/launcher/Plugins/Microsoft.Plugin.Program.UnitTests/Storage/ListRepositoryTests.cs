﻿// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using NUnit.Framework;
using Wox.Infrastructure.Storage;

namespace Microsoft.Plugin.Program.UnitTests.Storage
{
    [TestFixture]
    public class ListRepositoryTests
    {

        [Test]
        public void ContainsShouldReturnTrueWhenListIsInitializedWithItem()
        {
            // Arrange
            var itemName = "originalItem1";
            IRepository<string> repository = new ListRepository<string>() { itemName };

            // Act
            var result = repository.Contains(itemName);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsShouldReturnTrueWhenListIsUpdatedWithAdd()
        {
            // Arrange
            IRepository<string> repository = new ListRepository<string>();

            // Act
            var itemName = "newItem";
            repository.Add(itemName);
            var result = repository.Contains(itemName);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainsShouldReturnFalseWhenListIsUpdatedWithRemove()
        {
            // Arrange
            var itemName = "originalItem1";
            IRepository<string> repository = new ListRepository<string>() { itemName };

            // Act
            repository.Remove(itemName);
            var result = repository.Contains(itemName);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AddShouldNotThrowWhenBeingIterated()
        {
            // Arrange
            ListRepository<string> repository = new ListRepository<string>();
            var numItems = 1000;
            for (var i = 0; i < numItems; ++i)
            {
                repository.Add($"OriginalItem_{i}");
            }

            // Act - Begin iterating on one thread
            var iterationTask = Task.Run(() =>
            {
                var remainingIterations = 10000;
                while (remainingIterations > 0)
                {
                    foreach (var item in repository)
                    {
                        // keep iterating

                    }
                    --remainingIterations;
                }

            });

            // Act - Insert on another thread
            var addTask = Task.Run(() =>
           {
               for (var i = 0; i < numItems; ++i)
               {
                   repository.Add($"NewItem_{i}");
               }
           });

            // Assert that this does not throw.  Collections that aren't syncronized will throw an invalidoperatioexception if the list is modified while enumerating
            await Task.WhenAll(new Task[] { iterationTask, addTask }).ConfigureAwait(false);
        }

        [Test]
        public async Task RemoveShouldNotThrowWhenBeingIterated()
        {
            // Arrange
            ListRepository<string> repository = new ListRepository<string>();
            var numItems = 1000;
            for (var i = 0; i < numItems; ++i)
            {
                repository.Add($"OriginalItem_{i}");
            }

            // Act - Begin iterating on one thread
            var iterationTask = Task.Run(() =>
            {
                var remainingIterations = 10000;
                while (remainingIterations > 0)
                {
                    foreach (var item in repository)
                    {
                        // keep iterating

                    }
                    --remainingIterations;
                }

            });

            // Act - Remove on another thread
            var addTask = Task.Run(() =>
            {
                for (var i = 0; i < numItems; ++i)
                {
                    repository.Remove($"OriginalItem_{i}");
                }
            });

            // Assert that this does not throw.  Collections that aren't syncronized will throw an invalidoperatioexception if the list is modified while enumerating
            await Task.WhenAll(new Task[] { iterationTask, addTask }).ConfigureAwait(false);
        }
    }
}
