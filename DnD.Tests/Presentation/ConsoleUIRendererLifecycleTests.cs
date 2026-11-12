using System;
using System.IO;
using DnD.Core;
using DnD.Presentation;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DnD.Tests.Presentation
{
    public class ConsoleUIRendererLifecycleTests
    {
        [Fact]
        public void Dispose_CanBeCalledRepeatedlyAndStopsRenderingEvents()
        {
            var originalOutput = Console.Out;
            using var output = new StringWriter();
            GameEvents.ClearAll();
            Console.SetOut(output);
            ConsoleUIRenderer renderer = null;
            try
            {
                renderer = new ConsoleUIRenderer();

                renderer.Dispose();
                renderer.Dispose();
                GameEvents.RaiseCombatLog("after dispose");

                Assert.Equal(string.Empty, output.ToString());
            }
            finally
            {
                renderer?.Dispose();
                GameEvents.ClearAll();
                Console.SetOut(originalOutput);
            }
        }

        [Fact]
        public void ClearAll_RemovesActiveRendererSubscriptionsAndDisposeRemainsSafe()
        {
            var originalOutput = Console.Out;
            using var output = new StringWriter();
            GameEvents.ClearAll();
            Console.SetOut(output);
            ConsoleUIRenderer renderer = null;
            try
            {
                renderer = new ConsoleUIRenderer();
                GameEvents.RaiseCombatLog("before clear");
                var outputBeforeClear = output.ToString();

                GameEvents.ClearAll();
                renderer.Dispose();
                GameEvents.RaiseCombatLog("after clear");

                Assert.Contains("before clear", outputBeforeClear);
                Assert.Equal(outputBeforeClear, output.ToString());
            }
            finally
            {
                renderer?.Dispose();
                GameEvents.ClearAll();
                Console.SetOut(originalOutput);
            }
        }
    }
}
