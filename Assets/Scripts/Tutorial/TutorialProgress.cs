using System;
using UnityEngine;

namespace Possession
{
    [Flags]
    public enum TutorialMovement { None = 0, Forward = 1, Back = 2, Left = 4, Right = 8, Jump = 16, All = 31 }
    public enum TutorialLesson { Welcome, Movement, FindGuard, ShootTarget, ManualEject, RepossessGuard,
        WaitForTimer, EscapeHostile, FindHeavy, SmashWall, ReachExit, Complete, Failed }
    public enum TutorialFocus { None, LevelClock, Health, GuardAbility, HeavyAbility, PossessionClock }

    // Tracks demonstrated skills; acting early never loses credit or skips another skill.
    public sealed class TutorialProgress
    {
        public bool Started { get; private set; }
        public TutorialMovement Movement { get; private set; }
        public bool UsedGuard { get; private set; }
        public bool HitTarget { get; private set; }
        public bool EjectedManually { get; private set; }
        public bool ForcedOut { get; private set; }
        public bool SurvivedHostility { get; private set; }
        public bool UsedHeavy { get; private set; }
        public bool BrokeWall { get; private set; }
        public bool Completed { get; private set; }
        public bool Failed { get; private set; }
        public PlayerState Form { get; private set; } = PlayerState.Spirit;
        public bool CanFinish => Started && HitTarget && BrokeWall && !Failed;
        public TutorialMovement NextMovement
        {
            get
            {
                foreach (var direction in new[] { TutorialMovement.Forward, TutorialMovement.Left,
                    TutorialMovement.Back, TutorialMovement.Right, TutorialMovement.Jump })
                    if ((Movement & direction) == 0) return direction;
                return TutorialMovement.None;
            }
        }
        public int ClearedSkills => (Movement == TutorialMovement.All ? 1 : 0) + (UsedGuard ? 1 : 0)
            + (HitTarget ? 1 : 0) + (EjectedManually ? 1 : 0) + (ForcedOut ? 1 : 0)
            + (SurvivedHostility ? 1 : 0) + (UsedHeavy ? 1 : 0) + (BrokeWall ? 1 : 0) + (Completed ? 1 : 0);

        public TutorialLesson Lesson
        {
            get
            {
                if (Failed) return TutorialLesson.Failed;
                if (Completed) return TutorialLesson.Complete;
                if (!Started) return TutorialLesson.Welcome;
                if (Form == PlayerState.Spirit && ForcedOut && !SurvivedHostility) return TutorialLesson.EscapeHostile;
                if (HitTarget && BrokeWall) return TutorialLesson.ReachExit;
                // Teach the controlled body's ability even if earlier optional lessons were skipped.
                if (Form == PlayerState.Possessed_Heavy) return BrokeWall ? TutorialLesson.FindGuard : TutorialLesson.SmashWall;
                if (Form == PlayerState.Possessed_Guard) return !HitTarget ? TutorialLesson.ShootTarget
                    : !ForcedOut ? TutorialLesson.WaitForTimer : TutorialLesson.ManualEject;
                if (HitTarget) return TutorialLesson.FindHeavy;
                if (!UsedGuard && Movement != TutorialMovement.All) return TutorialLesson.Movement;
                return TutorialLesson.FindGuard;
            }
        }

        public void Begin() => Started = true;
        public void Move(TutorialMovement movement) { if (Started && !Failed) Movement |= movement; }
        public void Possessed(PlayerState form)
        {
            if (!Started || Failed) return;
            Form = form;
            if (form == PlayerState.Possessed_Guard) UsedGuard = true;
            if (form == PlayerState.Possessed_Heavy) UsedHeavy = true;
        }
        public void Ejected(bool fromGuard, bool forced)
        {
            if (!Started || Failed) return;
            Form = PlayerState.Spirit;
            if (forced) { ForcedOut = true; SurvivedHostility = false; }
            else EjectedManually = true;
        }
        public void TargetWasHit() { if (Started && !Failed) HitTarget = true; }
        public void WallWasBroken() { if (Started && !Failed) BrokeWall = true; }
        public void HostilityEnded() { if (ForcedOut && !Failed) SurvivedHostility = true; }
        public void Finish() { if (CanFinish && Form == PlayerState.Spirit) Completed = true; }
        public void Fail() { if (!Completed) Failed = true; }
    }

    public static class TutorialScene
    {
        public const string Name = "Tutorial";
        public const string Path = "Assets/Scenes/Tutorial.unity";
    }
}
