using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Possession.Tests
{
    public class BarChartTests
    {
        private readonly List<GameObject> objects = new();
        private ResultsView view;
        private RectTransform chartContainer;

        [SetUp]
        public void SetUp()
        {
            var canvas = Create("Canvas");
            canvas.AddComponent<Canvas>();

            var panel = Create("ResultsPanel");
            panel.transform.SetParent(canvas.transform, false);
            view = panel.AddComponent<ResultsView>();

            var container = Create("ChartContainer");
            container.transform.SetParent(panel.transform, false);
            chartContainer = container.AddComponent<RectTransform>();
            chartContainer.sizeDelta = new Vector2(800f, 36f);

            typeof(ResultsView).GetField("chartContainer",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(view, chartContainer);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
        }

        [Test]
        public void ShowChartCreatesOnBarPerEntry()
        {
            var entries = new List<PossessionEntry>
            {
                new PossessionEntry { enemyType = "Guard", duration = 4f, forcedEject = false },
                new PossessionEntry { enemyType = "Heavy", duration = 6f, forcedEject = true }
            };
            view.ShowChart(entries);
            Assert.That(chartContainer.childCount, Is.EqualTo(2));
        }

        [Test]
        public void GuardBarIsBlueAndHeavyBarIsOrange()
        {
            var entries = new List<PossessionEntry>
            {
                new PossessionEntry { enemyType = "Guard", duration = 3f, forcedEject = false },
                new PossessionEntry { enemyType = "Heavy", duration = 3f, forcedEject = false }
            };
            view.ShowChart(entries);

            var guardBar = chartContainer.GetChild(0).GetComponent<Image>();
            var heavyBar = chartContainer.GetChild(1).GetComponent<Image>();
            Assert.That(guardBar.color.b, Is.GreaterThan(guardBar.color.r),
                "Guard bar should be blue-dominant");
            Assert.That(heavyBar.color.r, Is.GreaterThan(heavyBar.color.b),
                "Heavy bar should be orange/red-dominant");
        }

        [Test]
        public void BarWidthsAreProportionalToDuration()
        {
            var entries = new List<PossessionEntry>
            {
                new PossessionEntry { enemyType = "Guard", duration = 2f, forcedEject = false },
                new PossessionEntry { enemyType = "Guard", duration = 8f, forcedEject = false }
            };
            view.ShowChart(entries);

            float w0 = chartContainer.GetChild(0).GetComponent<RectTransform>().sizeDelta.x;
            float w1 = chartContainer.GetChild(1).GetComponent<RectTransform>().sizeDelta.x;
            Assert.That(w1, Is.GreaterThan(w0 * 3f),
                "8s bar should be roughly 4x wider than the 2s bar");
        }

        [Test]
        public void SinglePossessionFillsFullWidth()
        {
            var entries = new List<PossessionEntry>
            {
                new PossessionEntry { enemyType = "Guard", duration = 5f, forcedEject = false }
            };
            view.ShowChart(entries);

            float w = chartContainer.GetChild(0).GetComponent<RectTransform>().sizeDelta.x;
            Assert.That(w, Is.EqualTo(800f).Within(1f),
                "Only bar should fill the full container width");
        }

        [Test]
        public void EmptyListCreatesNoBars()
        {
            view.ShowChart(new List<PossessionEntry>());
            Assert.That(chartContainer.childCount, Is.EqualTo(0));
        }

        [Test]
        public void NullListDoesNotThrow()
        {
            Assert.DoesNotThrow(() => view.ShowChart(null));
            Assert.That(chartContainer.childCount, Is.EqualTo(0));
        }

        [Test]
        public void CallingShowChartTwiceClearsOldBars()
        {
            var first = new List<PossessionEntry>
            {
                new PossessionEntry { enemyType = "Guard", duration = 3f, forcedEject = false },
                new PossessionEntry { enemyType = "Heavy", duration = 3f, forcedEject = false }
            };
            view.ShowChart(first);
            Assert.That(chartContainer.childCount, Is.EqualTo(2));

            var second = new List<PossessionEntry>
            {
                new PossessionEntry { enemyType = "Guard", duration = 5f, forcedEject = false }
            };
            view.ShowChart(second);
            Assert.That(chartContainer.childCount, Is.EqualTo(1));
        }

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            objects.Add(obj);
            return obj;
        }
    }
}
