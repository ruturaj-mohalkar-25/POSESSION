using System;
using UnityEngine;
namespace Possession
{
    // Local cosmetics never alter campaign progress, abilities or NPC uniforms.
    public static class PlayerAvatarSelection
    {
        public const string Key = "Possession.Avatar.Outfit";
        public static readonly string[] Names = { "CLASSIC", "VARSITY\nJACKET", "CARDINAL\nHOODIE", "SPORT\nJACKET" };
        public static event Action Changed;
        public static int Selected => Valid(PlayerPrefs.GetInt(Key));
        public static int Valid(int index) => index >= 0 && index < Names.Length ? index : 0;
        public static Color Cardinal => new Color(.60f,0f,.105f);
        public static Color Gold => new Color(1f,.80f,.20f);
        public static Color Cloth(int index) => index == 1 || index == 2 ? Cardinal : index == 3 ? new Color(.055f,.11f,.22f) : new Color(.91f,.94f,.93f);
        public static void Equip(int index)
        {
            PlayerPrefs.SetInt(Key,Valid(index));PlayerPrefs.Save();Changed?.Invoke();
        }
    }
}
