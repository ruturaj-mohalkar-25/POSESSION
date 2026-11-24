using System.Collections.Generic;
using UnityEngine;

namespace Possession.Level04Gold
{
    // Hides only upper-floor rendering. Supports, walls, navigation and gameplay remain active.
    [DefaultExecutionOrder(2000), DisallowMultipleComponent]
    public sealed class Level4FloorVisibility : MonoBehaviour
    {
        [SerializeField] private Transform upperRoot;
        private PossessionController possession;
        private readonly Dictionary<Renderer,bool> originalForceOff = new Dictionary<Renderer,bool>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Renderer> removed = new List<Renderer>();
        public bool UpperVisible { get; private set; } = true;
        public Transform UpperRoot => upperRoot;
        public Transform TrackedTarget { get; private set; }

        public void Configure(Transform root)
        {
            Restore();upperRoot=root;
        }

        private void Start() => RefreshVisibility();
        private void LateUpdate() => RefreshVisibility();

        public void RefreshVisibility()
        {
            if(upperRoot==null)return;
            if(possession==null)possession=FindFirstObjectByType<PossessionController>();
            TrackedTarget=possession==null?null:possession.PossessedEnemy!=null
                ?possession.PossessedEnemy.transform:possession.transform;
            if(TrackedTarget==null)return;
            // Hysteresis prevents flickering while walking on a stair tread or jumping.
            // A normal Spirit jump peaks below 4.7 m; it must not reveal an overhead floor.
            if(TrackedTarget.position.y>5.4f)UpperVisible=true;
            else if(TrackedTarget.position.y<4.9f)UpperVisible=false;

            // Health/charge visuals and enemy cues are created during Start and later
            // at runtime. Include them immediately instead of flashing them for a frame.
            renderers.Clear();upperRoot.GetComponentsInChildren(true,renderers);
            foreach(var renderer in renderers)
                if(renderer!=null&&!originalForceOff.ContainsKey(renderer))
                {
                    var avatar=renderer.GetComponent<CharacterAvatarView>();
                    originalForceOff.Add(renderer,avatar!=null?avatar.OriginalBodyRenderingOff:renderer.forceRenderingOff);
                }
            removed.Clear();
            foreach(var pair in originalForceOff)
            {
                var renderer=pair.Key;
                if(renderer==null){removed.Add(renderer);continue;}
                bool hidden=!UpperVisible;
                // A possessed upper-floor Heavy may descend the stairs while staying parented
                // beneath Upper floor. Its visible body must follow its physical floor.
                var enemy=renderer.GetComponentInParent<EnemyBase>();
                if(enemy!=null)hidden=!UpperVisible&&enemy.transform.position.y>3.1f;
                if(renderer.transform.IsChildOf(TrackedTarget))hidden=false;
                var avatar=renderer.GetComponent<CharacterAvatarView>();
                renderer.forceRenderingOff=hidden||pair.Value||avatar!=null&&avatar.isActiveAndEnabled;
            }
            foreach(var renderer in removed)originalForceOff.Remove(renderer);
        }

        private void Restore()
        {
            foreach(var pair in originalForceOff)if(pair.Key!=null)
            {
                var avatar=pair.Key.GetComponent<CharacterAvatarView>();
                pair.Key.forceRenderingOff=pair.Value||avatar!=null&&avatar.isActiveAndEnabled;
            }
            originalForceOff.Clear();renderers.Clear();
        }
        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
