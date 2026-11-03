using UnityEngine;

namespace Possession
{
    // Presentation only; the shared completion event and HUD remain owned by Level03Exit/GameManager.
    public sealed class Level03ExtractionBeacon : MonoBehaviour
    {
        [SerializeField] private Renderer[] signals;
        [SerializeField] private Transform ring;
        [SerializeField] private Light lamp;
        [SerializeField] private TextMesh status;
        private Level03Exit exit;
        private MaterialPropertyBlock block;
        private static readonly int ColorId=Shader.PropertyToID("_Color");
        private void Awake(){exit=GetComponent<Level03Exit>();block=new MaterialPropertyBlock();}
        private void Update()
        {
            bool ready=exit.IsReady;
            Color color=ready?new Color(.2f,1f,.65f):new Color(1f,.63f,.16f);
            float pulse=1f+Mathf.Sin(Time.time*2.2f)*.025f;
            ring.localScale=Vector3.one*pulse;
            block.SetColor(ColorId,color);
            foreach(var signal in signals)
            {
                signal.SetPropertyBlock(block);
                if(signal is LineRenderer line)line.startColor=line.endColor=color;
            }
            lamp.color=color;lamp.intensity=ready?2.2f:1f;
            status.color=color;status.text=ready?"EXTRACTION READY":"EXTRACTION · RESTORE ACCESS";
        }
    }
}
