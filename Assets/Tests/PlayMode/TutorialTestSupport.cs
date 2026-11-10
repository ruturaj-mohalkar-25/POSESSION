using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Possession.Tests
{
    internal static class TutorialTestSupport
    {
        internal static IEnumerator PreparePossession(TutorialGuide guide, EnemyBase body)
        {
            Dismiss(guide);
            var pc = guide.Possession;
            Assert.That(pc.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(pc.enabled, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            body.MoveSpeed = 0f;
            var charge = pc.GetComponent<PossessionCharge>();
            if (charge != null && !charge.HasCharge)
                Assert.That(charge.TryCollect(), Is.True, "The fixture must supply a charge before possession.");

            float deadline = Time.realtimeSinceStartup + 6f;
            while ((!pc.CanStartPossession || !body.CanBePossessed) && Time.realtimeSinceStartup < deadline)
            {
                Dismiss(guide);
                yield return null;
            }
            Assert.That(pc.CanStartPossession, Is.True, "Wait for the real ejection cooldown.");
            Assert.That(body.CanBePossessed, Is.True, "The chosen host must be alive and no longer hostile.");
            Approach(guide, body);
        }

        internal static IEnumerator Possess(TutorialGuide guide, EnemyBase body, bool dismissCoaching = true)
        {
            yield return PreparePossession(guide, body);
            Assert.That(guide.Possession.PossessDuration, Is.EqualTo(12f));
            typeof(PossessionController).GetMethod("Possess", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(guide.Possession, new object[] { body });
            Assert.That(guide.Possession.PossessedEnemy, Is.SameAs(body), "The tutorial must really possess its host.");
            if (dismissCoaching) Dismiss(guide);
        }

        internal static void Approach(TutorialGuide guide, EnemyBase body)
        {
            body.MoveSpeed = 0f;
            foreach (Vector3 direction in new[] { -body.transform.forward, Vector3.right,
                Vector3.back, Vector3.left, Vector3.forward })
            {
                Vector3 at = body.transform.position + direction * 2.5f;
                at.y = 1.15f;
                if (Physics.OverlapCapsule(at + Vector3.up * 0.45f, at - Vector3.up * 0.45f,
                    0.5f, 1 << 0, QueryTriggerInteraction.Ignore)
                    .Any(c => !c.transform.IsChildOf(guide.Spirit.transform))) continue;
                guide.Spirit.RestoreAt(at);
                Physics.SyncTransforms();
                var nearest = typeof(PossessionController).GetMethod("FindNearestEnemy",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guide.Possession, null);
                if (nearest == (object)body) return;
            }
            Assert.Fail("No clear approach outside the vision cone for " + body.name);
        }

        internal static IEnumerator WaitFor(Func<bool> ready, float timeout, string failure)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!ready() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(ready(), Is.True, failure);
        }

        private static void Dismiss(TutorialGuide guide)
        {
            for (int i = 0; i < 5 && guide.IsCoaching; i++) guide.ContinueCoaching();
            Assert.That(guide.IsCoaching, Is.False);
        }
    }
}
