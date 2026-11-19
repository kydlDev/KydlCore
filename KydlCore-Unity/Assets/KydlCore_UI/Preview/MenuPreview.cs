using KydlCore.Ui;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KydlCore.Ui
{
    /// <summary>
    /// Unity-only preview of the mod's menu (NOT compiled into the mod: only Runtime/ is). Add this component to any GameObject of an empty
    /// scene and press Play: the menu opens in the Game view in the same look as in game (game fonts from Assets/KydlCore.Ui/Preview/GameAssets).
    /// Set the Game view to 1920x1080 (the panel scales from that like in game).
    /// </summary>
    public sealed class MenuPreview : MonoBehaviour
    {
        /// <summary>What the preview shows: the KY hub (fake mods with 1, 3 and 4 tabs) or the example menu.</summary>
        public enum PreviewKind { Hub, HubManyTabs, MainMenu, ItemGrid }

        public PreviewKind kind = PreviewKind.Hub;
        HubView _hub;

        const string FontFolder = "Assets/KydlCore.Ui/Preview/GameAssets/";
        // NotoSansSC-Regular (8.5 MB, D-043): only KydlCore-Unity has it, OUTSIDE Assets/KydlCore.Ui (KydlUiSync mirrors that folder into every Unity project).
        const string KydlFontFolder = "Assets/KydlCore_UI/Preview/PreviewFonts/";

        void OnEnable()
        {
            LoadGameFonts();
            UiKit.Reset();
            var root = UiKit.Root;
            if (root == null) { Debug.LogError("[MenuPreview] UI Toolkit panel could not be made"); return; }
            if (kind == PreviewKind.Hub || kind == PreviewKind.HubManyTabs || kind == PreviewKind.ItemGrid)
            {
                var model = kind == PreviewKind.HubManyTabs ? FakeHubMany() : FakeHub();
                bool grid = kind == PreviewKind.ItemGrid;
                _hub = new HubView(model, UiText.English, (mod, tab) => grid ? FakeGridPage() : FakePage(), () => Debug.Log("[MenuPreview] close pressed"));
                root.Add(_hub.Root);
                Debug.Log("[MenuPreview] KY hub shown (fake mods)");
                return;
            }
            root.Add(MainMenuView.Build(UiText.English, () => Debug.Log("[MenuPreview] close pressed")));
            Debug.Log("[MenuPreview] KydlCore menu shown");
        }

        void OnDisable()
        {
            if (_hub != null) { _hub.Dispose(); _hub = null; }
            UiKit.Reset();
        }

        // Many mods and many tabs (T-035): row 1 and row 2 fill more than 2 lines, one title is longer than the window.
        static HubModel FakeHubMany()
        {
            var m = new HubModel();
            foreach (string n in new[] { "ChestDecor", "DrainTarPit", "KydlCore", "RearViewCamera", "SteveTheTroll", "ValheimDiscordRelay", "WhipItUp", "WaterAndTarPools", "FollowPlayer", "RidingCrop", "SpareMod", "AnotherSpareMod", "TheModWithAnExtremelyLongNameThatDoesNotFitInOneLineOfTheWindowAtAll" })
                m.AddMod("com.kydldev." + n, n);
            string g = "com.kydldev.ChestDecor";
            string[] tabs = { "Look", "Placement", "Camera", "General", "Appearance", "Advanced", "Keys", "Colours", "Storage", "Sorting", "Sounds", "Effects", "Language", "About" };
            for (int i = 0; i < tabs.Length; i++) m.AddTab(g, tabs[i].ToLowerInvariant(), tabs[i], i);
            m.Select(g, "placement");
            return m;
        }

        // Fake mods for the hub: 4 tabs, 1 tab, no registered tab (= the automatic "Settings" tab), 3 tabs. Names are PascalCase, as in game.
        static HubModel FakeHub()
        {
            var m = new HubModel();
            m.AddMod("com.kydldev.ChestDecor", "ChestDecor");
            m.AddTab("com.kydldev.ChestDecor", "look", "Look", 0);
            m.AddTab("com.kydldev.ChestDecor", "placement", "Placement", 1);
            m.AddTab("com.kydldev.ChestDecor", "camera", "Camera", 2);
            m.AddTab("com.kydldev.ChestDecor", "general", "General", 3);
            m.AddMod("com.kydldev.DrainTarPit", "DrainTarPit");
            m.AddTab("com.kydldev.DrainTarPit", "general", "General", 0);
            m.AddMod("com.kydldev.KydlCore", "KydlCore");
            m.AddMod("com.kydldev.RearViewCamera", "RearViewCamera");
            m.AddTab("com.kydldev.RearViewCamera", "camera", "Camera", 0);
            m.AddTab("com.kydldev.RearViewCamera", "image", "Image", 1);
            m.AddTab("com.kydldev.RearViewCamera", "keys", "Keys", 2);
            m.Select("com.kydldev.ChestDecor", "placement");
            return m;
        }

        // The KY-Items grid (D-096): 6 fake prefabs, two rows of the 4-per-row grid, round coloured icons on a transparent background
        static VisualElement FakeGridPage()
        {
            var page = new VisualElement();
            page.Add(UiControls.Note("6 prefab(s) of 1 source(s). Items and pieces the KydlDev mods contribute."));
            page.Add(UiControls.Heading("com.kydldev.GunPowdered"));
            var cards = new System.Collections.Generic.List<ItemCard>();
            string[] names = { "Bucket", "Bucket (full)", "Iron Musket", "Cartridge", "Spiked Grenade", "A name long enough to wrap onto a second line" };
            Color[] colours = { new Color(0.6f, 0.4f, 0.2f), new Color(0.3f, 0.5f, 0.8f), new Color(0.3f, 0.3f, 0.3f), new Color(0.9f, 0.85f, 0.7f), new Color(0.35f, 0.33f, 0.3f), new Color(0.8f, 0.2f, 0.2f) };
            for (int i = 0; i < names.Length; i++) cards.Add(new ItemCard { Title = names[i], Subtitle = "GP_Example" + i, Icon = FakeIcon(colours[i]) });
            page.Add(ItemGridView.Build(cards));
            return page;
        }

        static Sprite FakeIcon(Color c)
        {
            const int s = 64;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f, s / 2f));
                tex.SetPixel(x, y, d < s * 0.4f ? c : new Color(0, 0, 0, 0));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f));
        }

        static VisualElement FakePage()
        {
            var page = new VisualElement();
            page.Add(UiControls.Toggle("Example switch", true, on => Debug.Log("[MenuPreview] switch " + on)));
            page.Add(UiControls.Row("Example value", UiControls.Slider(0f, 10f, 5f, "0.0", v => Debug.Log("[MenuPreview] slider " + v))));
            return page;
        }

        // UiKit finds the game fonts by name among the loaded fonts (NotoSansSC-Regular, NotoEmoji-Regular; the old Norse / Norsebold for the
        // build without KYDL_CORE_PUBLIC): load them first, like the game has them loaded.
        static void LoadGameFonts()
        {
#if UNITY_EDITOR
            foreach (var f in new[] { "Norse.otf", "Norsebold.otf", "NotoEmoji-Regular.ttf" })
                if (AssetDatabase.LoadAssetAtPath<Font>(FontFolder + f) == null) Debug.LogWarning("[MenuPreview] font missing: " + FontFolder + f + " (Kydl > Sync shared UI kit)");
            const string noto = "NotoSansSC-Regular.otf";
            var font = AssetDatabase.LoadAssetAtPath<Font>(KydlFontFolder + noto);
            if (font == null) Debug.LogWarning("[MenuPreview] font missing: " + KydlFontFolder + noto + " (copy it from the ripped game, Assets/Font)");
            else Debug.Log("[MenuPreview] font loaded: '" + font.name + "' from " + KydlFontFolder + noto);
#endif
        }
    }
}
