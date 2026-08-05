using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MochiMeadows.Art;
using MochiMeadows.Game;
using MochiMeadows.Inputs;
using MochiMeadows.Audio;

namespace MochiMeadows.Ui
{
    // All UI is built at runtime (no prefabs), scaled for any screen.
    public class UiController : MonoBehaviour
    {
        public Canvas Canvas { get; private set; }
        public bool IsFishing => fishingActive;
        public RectTransform Root { get; private set; }

        Font font;
        Image fadeImage;
        Image[] energyHearts = new Image[10];
        Text dayText, clockText, coinText, toastText;
        Image[] hotbarSlots = new Image[9];
        Image[] hotbarIcons = new Image[9];
        Text[] hotbarCounts = new Text[9];
        Text toastTitle;
        RectTransform hotbarRect;

        // shop
        GameObject shopPanel;
        Text shopCoins;
        RectTransform basketList;
        List<GameObject> shopRows = new List<GameObject>();
        GameObject questPanel;
        GameObject makeoverPanel;
        GameObject fishPanel;
        GameObject cookPanel;
        RectTransform cookList;
        List<GameObject> cookRows = new List<GameObject>();
        GameObject decorPanel;
        RectTransform decorList;
        List<GameObject> decorRows = new List<GameObject>();
        GameObject scrapPanel;
        RectTransform scrapList;
        List<GameObject> scrapRows = new List<GameObject>();
        RectTransform fishZone, fishCursor;
        Image fishZoneImg;
        Text fishTimer;
        bool fishingActive;
        float fishingT;
        bool fishingDone;
        bool fishingTapQueued;
        List<GameObject> makeoverRows = new List<GameObject>();
        RectTransform questList;
        List<GameObject> questRows = new List<GameObject>();
        Button menuSaveBtn, menuQuitBtn, menuResumeBtn, menuTitleBtn;
        GameObject controlsPanel;
        GameObject newGamePanel;

        // speech bubble
        Canvas bubbleCanvas;
        RectTransform bubbleRect;
        Text bubbleText;

        // joystick
        Image joystickBase, joystickKnob;
        bool joystickActive;
        int joystickFinger = -1;
        Vector2 joystickOrigin;

        // letterbox
        Image barLeft, barRight, barTop, barBottom;

        // title / menu
        GameObject titlePanel, menuPanel;
        GameObject hudRoot;

        Color barColor = new Color(0.70f, 0.62f, 0.85f); // soft lavender

        public void BuildUi()
        {
            font = FontHelper.Get();

            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);
            Canvas = canvasGo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasGo.AddComponent<GraphicRaycaster>();

            Root = canvasGo.GetComponent<RectTransform>();

            EnsureEventSystem();
            BuildLetterbox();
            BuildHud();
            BuildBubble();
            BuildJoystick();
            BuildShop();
            BuildQuestPanel();
            BuildMakeover();
            BuildFishing();
            BuildCooking();
            BuildDecor();
            BuildScrapbook();
            BuildTitle();
            BuildNewGame();
            BuildMenu();
            BuildControls();

            // respond to game events
            var gm = GameManager.I;
            gm.OnMinuteTick += () => RefreshClock();
            gm.OnNewDay += () => RefreshClock();
            gm.OnMoneyChanged += () => RefreshCoins();
            gm.OnEnergyChanged += () => RefreshEnergy();
            gm.OnToast += ShowToast;
            gm.OnBasketChanged += () => RefreshShop();
            RefreshAll();
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        void BuildLetterbox()
        {
            barLeft = CreateImage(Root, "BarLeft", null, barColor);
            barRight = CreateImage(Root, "BarRight", null, barColor);
            barTop = CreateImage(Root, "BarTop", null, barColor);
            barBottom = CreateImage(Root, "BarBottom", null, barColor);
            foreach (var b in new[] { barLeft, barRight, barTop, barBottom })
                b.raycastTarget = false;
            UpdateLetterbox();
        }

        public void UpdateLetterbox()
        {
            if (barLeft == null) return;
            float aspect = Screen.width / (float)Screen.height;
            const float target = 16f / 9f;
            float pw = 0, ph = 0;
            if (aspect > target) { pw = (1f - target / aspect) * 0.5f; ph = 0; }
            else { ph = (1f - aspect / target) * 0.5f; pw = 0; }
            SetAnchors(barLeft, 0, pw, 0, 1);
            SetAnchors(barRight, 1 - pw, 1, 0, 1);
            SetAnchors(barTop, pw, 1 - pw, 1 - ph, 1);
            SetAnchors(barBottom, pw, 1 - pw, 0, ph);
        }

        void SetAnchors(Image img, float x0, float x1, float y0, float y1)
        {
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void BuildHud()
        {
            hudRoot = new GameObject("Hud");
            hudRoot.transform.SetParent(Root, false);
hudRoot = new GameObject("Hud");
            hudRoot.transform.SetParent(Root, false);
            var hudRt = hudRoot.AddComponent<RectTransform>();
            hudRt.anchorMin = Vector2.zero; hudRt.anchorMax = Vector2.one;
            hudRt.offsetMin = Vector2.zero; hudRt.offsetMax = Vector2.zero;

            // top-left: day badge
            var dayPanel = CreateImage(hudRoot.transform, "DayBadge", null, new Color(1, 1, 1, 0.82f));
            dayPanel.raycastTarget = false;
            SetAnchors(dayPanel, 0.012f, 0.012f, 0.955f, 0.955f);
            dayPanel.rectTransform.sizeDelta = new Vector2(330, 56);
            dayPanel.rectTransform.anchoredPosition = new Vector2(0, 0);
            dayPanel.rectTransform.pivot = new Vector2(0, 0.5f);
            dayPanel.sprite = RoundedSprite(Palette.Cream);
            dayPanel.type = Image.Type.Sliced;

            dayText = CreateText(dayPanel.transform, "DayText", "", 26, Palette.Chocolate);
            dayText.rectTransform.anchorMin = Vector2.zero;
            dayText.rectTransform.anchorMax = Vector2.one;
            dayText.rectTransform.offsetMin = new Vector2(20, 0);
            dayText.rectTransform.offsetMax = new Vector2(-10, 0);
            dayText.alignment = TextAnchor.MiddleLeft;

            clockText = CreateText(dayPanel.transform, "ClockText", "", 22, Palette.Chocolate);
            clockText.rectTransform.anchorMin = new Vector2(0, 0);
            clockText.rectTransform.anchorMax = new Vector2(1, 0);
            clockText.rectTransform.offsetMin = new Vector2(20, 2);
            clockText.rectTransform.offsetMax = new Vector2(-10, 34);
            clockText.alignment = TextAnchor.LowerLeft;

            // top-right: coins
            var coinPanel = CreateImage(hudRoot.transform, "Coins", null, new Color(1, 1, 1, 0.82f));
            coinPanel.raycastTarget = false;
            SetAnchors(coinPanel, 0.988f, 0.988f, 0.955f, 0.955f);
            coinPanel.rectTransform.pivot = new Vector2(1, 0.5f);
            coinPanel.rectTransform.sizeDelta = new Vector2(260, 56);
            coinPanel.sprite = RoundedSprite(Palette.Cream);
            coinPanel.type = Image.Type.Sliced;

            var coinIcon = CreateImage(coinPanel.transform, "CoinIcon", SpriteBank.Coin, Color.white);
            coinIcon.rectTransform.anchorMin = new Vector2(0, 0);
            coinIcon.rectTransform.anchorMax = new Vector2(0, 1);
            coinIcon.rectTransform.sizeDelta = new Vector2(44, 44);
            coinIcon.rectTransform.anchoredPosition = new Vector2(22, 0);
            coinIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            coinText = CreateText(coinPanel.transform, "CoinText", "", 26, Palette.Chocolate);
            coinText.rectTransform.anchorMin = Vector2.zero;
            coinText.rectTransform.anchorMax = Vector2.one;
            coinText.rectTransform.offsetMin = new Vector2(52, 0);
            coinText.rectTransform.offsetMax = new Vector2(-20, 0);
            coinText.alignment = TextAnchor.MiddleLeft;

            // top-center: energy hearts
            for (int i = 0; i < 10; i++)
            {
                var heart = CreateImage(hudRoot.transform, "Heart" + i, SpriteBank.Heart, Palette.DeepPink);
                heart.rectTransform.anchorMin = new Vector2(0.5f, 1);
                heart.rectTransform.anchorMax = new Vector2(0.5f, 1);
                heart.rectTransform.pivot = new Vector2(0.5f, 1);
                heart.rectTransform.anchoredPosition = new Vector2((i - 4.5f) * 46f, -12);
                heart.rectTransform.sizeDelta = new Vector2(40, 36);
                energyHearts[i] = heart;
            }

            // toast (center-top)
            var toastBg = CreateImage(hudRoot.transform, "ToastBg", RoundedSprite(Palette.Lavender), new Color(0.85f, 0.8f, 0.95f, 0.92f));
            toastBg.rectTransform.anchorMin = new Vector2(0.5f, 0.78f);
            toastBg.rectTransform.anchorMax = new Vector2(0.5f, 0.78f);
            toastBg.rectTransform.pivot = new Vector2(0.5f, 1);
            toastBg.rectTransform.sizeDelta = new Vector2(720, 78);
            toastBg.type = Image.Type.Sliced;
            toastBg.raycastTarget = false;
            toastBg.gameObject.SetActive(false);

            var toastGo = CreateText(toastBg.transform, "Toast", "", 30, Palette.Chocolate);
            AddOutline(toastGo, 1f, new Color(0.36f, 0.28f, 0.40f, 0.55f));
            toastGo.rectTransform.anchorMin = Vector2.zero;
            toastGo.rectTransform.anchorMax = Vector2.one;
            toastGo.rectTransform.offsetMin = new Vector2(24, 0);
            toastGo.rectTransform.offsetMax = new Vector2(-24, 0);
            toastGo.alignment = TextAnchor.MiddleCenter;
            toastGo.horizontalOverflow = HorizontalWrapMode.Wrap;
            toastGo.verticalOverflow = VerticalWrapMode.Truncate;
            toastText = toastGo;
            toastText.gameObject.SetActive(false);

            // hotbar
            hotbarRect = CreateImage(hudRoot.transform, "Hotbar", RoundedSprite(new Color(0.55f, 0.45f, 0.75f)), new Color(1, 1, 1, 0.65f)).rectTransform;
            hotbarRect.anchorMin = new Vector2(0.5f, 0);
            hotbarRect.anchorMax = new Vector2(0.5f, 0);
            hotbarRect.pivot = new Vector2(0.5f, 0);
            hotbarRect.anchoredPosition = new Vector2(0, 14);
            hotbarRect.sizeDelta = new Vector2(9 * 96 + 16, 100);
            hotbarRect.GetComponent<Image>().type = Image.Type.Sliced;
            hotbarRect.GetComponent<Image>().raycastTarget = false;

            for (int i = 0; i < 9; i++)
            {
                var slot = CreateImage(hotbarRect, "Slot" + i, RoundedSprite(Palette.Cream), Color.white);
                slot.rectTransform.anchorMin = new Vector2(0, 0);
                slot.rectTransform.anchorMax = new Vector2(0, 1);
                slot.rectTransform.pivot = new Vector2(0, 0.5f);
                slot.rectTransform.anchoredPosition = new Vector2(8 + i * 96f, 0);
                slot.rectTransform.sizeDelta = new Vector2(88, 88);
                slot.type = Image.Type.Sliced;
                hotbarSlots[i] = slot;

                var icon = CreateImage(slot.transform, "Icon", null, Color.white);
                icon.rectTransform.anchorMin = Vector2.zero;
                icon.rectTransform.anchorMax = Vector2.one;
                icon.rectTransform.offsetMin = new Vector2(10, 10);
                icon.rectTransform.offsetMax = new Vector2(-10, -10);
                icon.preserveAspect = true;
                hotbarIcons[i] = icon;

                var count = CreateText(slot.transform, "Count", "", 20, Palette.Chocolate);
                count.rectTransform.anchorMin = new Vector2(1, 0);
                count.rectTransform.anchorMax = new Vector2(1, 0);
                count.rectTransform.pivot = new Vector2(1, 0);
                count.rectTransform.anchoredPosition = new Vector2(-6, 4);
                count.rectTransform.sizeDelta = new Vector2(60, 24);
                count.alignment = TextAnchor.LowerRight;
                hotbarCounts[i] = count;

                int slotIdx = i;
                var btn = slot.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => GameManager.I.SelectHotbar(slotIdx));
            }

            // menu button top-right corner
            var menuBtn = CreateButton(hudRoot.transform, "MenuBtn", "☕", 28, Palette.Chocolate, out var menuBtnBg);
            Rt(menuBtn).anchorMin = new Vector2(0.5f, 1);
            Rt(menuBtn).anchorMax = new Vector2(0.5f, 1);
            Rt(menuBtn).pivot = new Vector2(0.5f, 1);
            Rt(menuBtn).anchoredPosition = new Vector2(0, 14);
            Rt(menuBtn).sizeDelta = new Vector2(90, 44);
            menuBtn.GetComponentInChildren<Text>().color = Color.white;
            menuBtnBg.sprite = RoundedSprite(Palette.Lavender);
            menuBtnBg.type = Image.Type.Sliced;
            menuBtnBg.color = new Color(0.72f, 0.6f, 0.9f, 0.9f);
            menuBtn.onClick.AddListener(() => OpenMenu());

            // fade overlay
            var fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(Root, false);
            fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.color = new Color(0.1f, 0.08f, 0.2f, 0);
            fadeImage.raycastTarget = false;
            fadeImage.rectTransform.anchorMin = Vector2.zero;
            fadeImage.rectTransform.anchorMax = Vector2.one;
            fadeImage.rectTransform.offsetMin = Vector2.zero;
            fadeImage.rectTransform.offsetMax = Vector2.zero;
            fadeImage.rectTransform.SetAsLastSibling();
        }

        void BuildBubble()
        {
            var go = new GameObject("SpeechBubbleCanvas");
            go.transform.SetParent(transform, false);
            bubbleCanvas = go.AddComponent<Canvas>();
            bubbleCanvas.renderMode = RenderMode.WorldSpace;
            bubbleCanvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            go.transform.localScale = new Vector3(0.006f, 0.006f, 0.006f);

            var bg = CreateImage(go.transform, "Bubble", RoundedSprite(Palette.White), Color.white);
            bubbleRect = bg.rectTransform;
            bubbleRect.sizeDelta = new Vector2(340, 90);
            bubbleRect.pivot = new Vector2(0.5f, 1);
            bubbleRect.anchoredPosition = new Vector2(0, 0);
            bg.type = Image.Type.Sliced;

            bubbleText = CreateText(go.transform, "BubbleText", "", 34, Palette.Chocolate);
            bubbleText.rectTransform.anchorMin = Vector2.zero;
            bubbleText.rectTransform.anchorMax = Vector2.one;
            bubbleText.rectTransform.offsetMin = new Vector2(16, 8);
            bubbleText.rectTransform.offsetMax = new Vector2(-16, -8);
            bubbleText.alignment = TextAnchor.MiddleCenter;
            bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;

            go.SetActive(false);
        }

        public void ShowBubble(Vector3 worldPos, string text, float duration = 2.6f)
        {
            StopAllCoroutines();
            bubbleCanvas.gameObject.SetActive(true);
            bubbleRect.anchoredPosition = new Vector2(0, 0.9f);
            bubbleText.text = text;
            float w = Mathf.Clamp(text.Length * 26f + 60f, 180f, 520f);
            bubbleRect.sizeDelta = new Vector2(w, 78);
            bubbleCanvas.transform.position = worldPos;
            StartCoroutine(HideBubbleRoutine(duration));
        }

        IEnumerator HideBubbleRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            bubbleCanvas.gameObject.SetActive(false);
        }

        void BuildJoystick()
        {
            bool mobile = Application.isMobilePlatform;
            float baseSize = mobile ? 240f : 180f;
            float knobSize = mobile ? 120f : 90f;

            joystickBase = CreateImage(Root, "JoystickBase", SpriteFactory.SoftCircle(64, Color.white), new Color(1, 1, 1, 0.18f));
            joystickBase.rectTransform.sizeDelta = new Vector2(baseSize, baseSize);
            joystickBase.gameObject.SetActive(false);

            joystickKnob = CreateImage(joystickBase.transform, "JoystickKnob", SpriteFactory.SoftCircle(48, Color.white), new Color(1, 1, 1, 0.35f));
            joystickKnob.rectTransform.sizeDelta = new Vector2(knobSize, knobSize);
            joystickKnob.rectTransform.anchoredPosition = Vector2.zero;

            if (mobile)
            {
                // idle pad hint so players see the control exists
                var hint = CreateImage(Root, "JoystickHint", SpriteFactory.SoftCircle(64, Color.white), new Color(1, 1, 1, 0.10f));
                hint.rectTransform.anchorMin = new Vector2(0, 0);
                hint.rectTransform.anchorMax = new Vector2(0, 0);
                hint.rectTransform.pivot = new Vector2(0, 0);
                hint.rectTransform.anchoredPosition = new Vector2(40, 40);
                hint.rectTransform.sizeDelta = new Vector2(baseSize, baseSize);
                hint.raycastTarget = false;

                // thumb-friendly Act button
                var actBtn = CreateButton(Root, "ActButton", "Act", 26, Color.white, out var actBg);
                Rt(actBtn).anchorMin = new Vector2(1, 0);
                Rt(actBtn).anchorMax = new Vector2(1, 0);
                Rt(actBtn).pivot = new Vector2(1, 0);
                Rt(actBtn).anchoredPosition = new Vector2(-46, 52);
                Rt(actBtn).sizeDelta = new Vector2(150, 150);
                actBg.sprite = SpriteFactory.SoftCircle(64, Color.white);
                actBg.color = new Color(0.85f, 0.65f, 0.75f, 0.55f);
                var actLabel = actBtn.GetComponentInChildren<Text>();
                actLabel.alignment = TextAnchor.MiddleCenter;
                actLabel.rectTransform.offsetMin = new Vector2(0, -14);
                actLabel.rectTransform.offsetMax = new Vector2(0, 14);
                actBtn.onClick.AddListener(() =>
                {
                    var input = InputService.I;
                    if (input != null) input.ActPressed = true;
                });
            }
        }

        void Update()
        {
            UpdateLetterbox();
            HandleTouch();
            UpdateFishing();

            var input = InputService.I;
            if (input != null && (input.ClosePressed || input.MenuPressed))
            {
                bool handled = false;
                if (menuPanel != null && menuPanel.activeSelf) { CloseMenu(); handled = true; }
                else if (controlsPanel != null && controlsPanel.activeSelf) { controlsPanel.SetActive(false); handled = true; }
                else if (questPanel != null && questPanel.activeSelf) { questPanel.SetActive(false); handled = true; }
                else if (makeoverPanel != null && makeoverPanel.activeSelf) { makeoverPanel.SetActive(false); handled = true; }
                else if (fishPanel != null && fishPanel.activeSelf && !fishingDone) { fishingDone = true; StartCoroutine(CloseFishing(0f)); handled = true; }
                else if (cookPanel != null && cookPanel.activeSelf) { cookPanel.SetActive(false); handled = true; }
                else if (scrapPanel != null && scrapPanel.activeSelf) { scrapPanel.SetActive(false); handled = true; }
                else if (shopPanel != null && shopPanel.activeSelf) { shopPanel.SetActive(false); handled = true; }
                else if (GameManager.I != null && GameManager.I.GameStarted) { OpenMenu(); handled = true; }
            }
        }

        void HandleTouch()
        {
            if (!Application.isMobilePlatform)
            {
                // desktop: mouse click acts as a tap (but not over UI)
                if (Input.GetMouseButtonDown(0) && !IsOverUi())
                    HandleWorldTap(Input.mousePosition);
                return;
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                Vector2 p = t.position;
                if (t.phase == TouchPhase.Began)
                {
                    if (IsOverUi()) continue;
                    if (p.x < Screen.width * 0.42f)
                    {
                        joystickFinger = t.fingerId;
                        joystickActive = true;
                        joystickOrigin = p;
                        joystickBase.transform.position = p;
                        joystickKnob.transform.position = p;
                        joystickBase.gameObject.SetActive(true);
                    }
                    else
                    {
                        HandleWorldTap(p);
                    }
                }
                else if (t.phase == TouchPhase.Moved && t.fingerId == joystickFinger)
                {
                    Vector2 delta = p - joystickOrigin;
                    float maxR = 80f;
                    if (delta.magnitude > maxR) delta = delta.normalized * maxR;
                    joystickKnob.transform.position = joystickOrigin + delta;
                    var input = InputService.I;
                    if (input != null) input.JoystickAxis = delta / maxR;
                }
                else if ((t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) && t.fingerId == joystickFinger)
                {
                    joystickFinger = -1;
                    joystickActive = false;
                    joystickBase.gameObject.SetActive(false);
                    var input = InputService.I;
                    if (input != null) input.JoystickAxis = Vector2.zero;
                }
            }
        }

        bool IsOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        void HandleWorldTap(Vector2 screenPos)
        {
            if (fishingActive)
            {
                fishingTapQueued = true;
                return;
            }
            var gm0 = GameManager.I;
            if (gm0 != null && gm0.DecorPlacing >= 0)
            {
                var cam0 = Camera.main;
                if (cam0 != null)
                {
                    Vector3 w = cam0.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam0.transform.position.z));
                    int wx = Mathf.FloorToInt(w.x), wy = Mathf.FloorToInt(w.y);
                    if (!gm0.TryPickupDecor(wx, wy))
                        gm0.PlaceDecor(wx, wy);
                }
                return;
            }
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            var gm = GameManager.I;
            if (gm == null) return;

            // tapping the quest board
            if (gm.Quests != null && Vector2.Distance(world, gm.Quests.BoardPos) < 1.5f)
            {
                gm.Ui.OpenQuests();
                return;
            }
            // tapping a critter = pet
            if (gm.Critters != null)
            {
                for (int i = 0; i < gm.Critters.Length; i++)
                {
                    if (gm.Critters[i] == null) continue;
                    if (Vector2.Distance(world, gm.Critters[i].transform.position) < 1.6f)
                    {
                        gm.Critters[i].Pet();
                        return;
                    }
                }
            }
            // tapping Mochi?
            if (gm.Mochi != null)
            {
                float d = Vector2.Distance(world, gm.Mochi.transform.position);
                if (d < 1.6f)
                {
                    var input = InputService.I;
                    if (input != null) input.TappedNpc = true;
                    return;
                }
            }
            // tapping blanket?
            if (Vector2.Distance(world, gm.Farm.BlanketPos) < 1.3f)
            {
                gm.SleepAtBlanket();
                return;
            }
            // tile tap -> action
            var plot = gm.Farm.WorldToPlot(world);
            if (gm.Farm.InBounds(plot.x, plot.y))
            {
                var input = InputService.I;
                if (input != null)
                {
                    input.TapTileX = plot.x;
                    input.TapTileY = plot.y;
                }
            }
        }

        void BuildShop()
        {
            shopPanel = new GameObject("ShopPanel");
            shopPanel.transform.SetParent(Root, false);
shopPanel = new GameObject("ShopPanel");
            shopPanel.transform.SetParent(Root, false);
            var shopRt = shopPanel.AddComponent<RectTransform>();
            shopRt.anchorMin = Vector2.zero; shopRt.anchorMax = Vector2.one;
            shopRt.offsetMin = Vector2.zero; shopRt.offsetMax = Vector2.zero;
            var backdrop = CreateImage(shopPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(shopPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(720, 780);
            panel.type = Image.Type.Sliced;

            // header
            var title = CreateText(panel.transform, "Title", "Mochi's Market", 40, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -24);
            title.rectTransform.sizeDelta = new Vector2(600, 50);
            title.alignment = TextAnchor.MiddleCenter;

            var mochiFace = CreateImage(panel.transform, "Mochi", SpriteBank.MochiIdle, Color.white);
            mochiFace.rectTransform.anchorMin = new Vector2(0.5f, 1);
            mochiFace.rectTransform.anchorMax = new Vector2(0.5f, 1);
            mochiFace.rectTransform.pivot = new Vector2(0.5f, 1);
            mochiFace.rectTransform.anchoredPosition = new Vector2(0, -80);
            mochiFace.rectTransform.sizeDelta = new Vector2(72, 72);

            shopCoins = CreateText(panel.transform, "Coins", "", 26, Palette.Chocolate);
            shopCoins.rectTransform.anchorMin = new Vector2(1, 1);
            shopCoins.rectTransform.anchorMax = new Vector2(1, 1);
            shopCoins.rectTransform.pivot = new Vector2(1, 1);
            shopCoins.rectTransform.anchoredPosition = new Vector2(-30, -26);
            shopCoins.rectTransform.sizeDelta = new Vector2(240, 40);
            shopCoins.alignment = TextAnchor.MiddleRight;

            var scrapBtn = CreateButton(panel.transform, "Scrapbook", "📖 Scrapbook", 20, Color.white, out var scrapBtnBg);
            Rt(scrapBtn).anchorMin = new Vector2(0.5f, 1);
            Rt(scrapBtn).anchorMax = new Vector2(0.5f, 1);
            Rt(scrapBtn).pivot = new Vector2(0.5f, 1);
            Rt(scrapBtn).anchoredPosition = new Vector2(-95, -104);
            Rt(scrapBtn).sizeDelta = new Vector2(170, 44);
            scrapBtnBg.color = new Color(0.9f, 0.82f, 0.7f, 1);
            scrapBtnBg.sprite = RoundedSprite(Palette.Peach);
            scrapBtnBg.type = Image.Type.Sliced;
            scrapBtn.onClick.AddListener(() => OpenScrapbook());

            var makeoverBtn = CreateButton(panel.transform, "Makeover", "✨ Makeover", 22, Color.white, out var makeoverBtnBg);
            Rt(makeoverBtn).anchorMin = new Vector2(0.5f, 1);
            Rt(makeoverBtn).anchorMax = new Vector2(0.5f, 1);
            Rt(makeoverBtn).pivot = new Vector2(0.5f, 1);
            Rt(makeoverBtn).anchoredPosition = new Vector2(0, -104);
            Rt(makeoverBtn).sizeDelta = new Vector2(170, 44);
            makeoverBtnBg.color = new Color(0.85f, 0.78f, 0.95f, 1);
            makeoverBtnBg.sprite = RoundedSprite(Palette.Lavender);
            makeoverBtnBg.type = Image.Type.Sliced;
            makeoverBtn.onClick.AddListener(() => { RefreshMakeover(); makeoverPanel.SetActive(true); });

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBtnBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBtnBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBtnBg.sprite = RoundedSprite(Palette.Blush);
            closeBtnBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => shopPanel.SetActive(false));

            // sections
            var seedsHeader = CreateText(panel.transform, "SeedsHeader", "~ Seed packets ~", 28, Palette.DeepPink);
            seedsHeader.rectTransform.anchorMin = new Vector2(0.5f, 1);
            seedsHeader.rectTransform.anchorMax = new Vector2(0.5f, 1);
            seedsHeader.rectTransform.pivot = new Vector2(0.5f, 1);
            seedsHeader.rectTransform.anchoredPosition = new Vector2(0, -170);
            seedsHeader.rectTransform.sizeDelta = new Vector2(600, 40);
            seedsHeader.alignment = TextAnchor.MiddleCenter;

            float y = -220;
            for (int i = 0; i < 6; i++)
            {
                var def = CropDef.All[i];
                var row = CreateImage(panel.transform, "SeedRow" + i, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(620, 66);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", SpriteBank.CropSprites[i, 3], Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(58, 58);
                icon.rectTransform.anchoredPosition = new Vector2(32, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", def.SeedName, 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(68, 0);
                name.rectTransform.offsetMax = new Vector2(-190, 0);
                name.alignment = TextAnchor.MiddleLeft;

                bool inSeason = def.Season == GameManager.I.CurrentSeason;
                var price = CreateText(row.transform, "Price", def.SeedPrice + " coins" + (inSeason ? "  ★" : ""), 22, Palette.DeepPink);
                price.rectTransform.anchorMin = new Vector2(0, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(-185, 0);
                price.rectTransform.offsetMax = new Vector2(-120, 0);
                price.alignment = TextAnchor.MiddleLeft;

                var buy = CreateButton(row.transform, "Buy", "Buy", 24, Color.white, out var buyBg);
                Rt(buy).anchorMin = new Vector2(1, 0);
                Rt(buy).anchorMax = new Vector2(1, 1);
                Rt(buy).pivot = new Vector2(1, 0.5f);
                Rt(buy).anchoredPosition = new Vector2(-10, 0);
                Rt(buy).sizeDelta = new Vector2(100, 46);
                buyBg.color = new Color(0.7f, 0.9f, 0.75f, 1);
                buyBg.sprite = RoundedSprite(Palette.Mint);
                buyBg.type = Image.Type.Sliced;
                CropId crop = (CropId)i;
                buy.onClick.AddListener(() => { GameManager.I.BuySeed(crop); RefreshShop(); });

                y -= 66;
            }

            // decor
            var decorHeader = CreateText(panel.transform, "DecorHeader", "~ Decor ~", 28, Palette.Lavender);
            decorHeader.rectTransform.anchorMin = new Vector2(0.5f, 1);
            decorHeader.rectTransform.anchorMax = new Vector2(0.5f, 1);
            decorHeader.rectTransform.pivot = new Vector2(0.5f, 1);
            decorHeader.rectTransform.anchoredPosition = new Vector2(0, y);
            decorHeader.rectTransform.sizeDelta = new Vector2(600, 36);
            decorHeader.alignment = TextAnchor.MiddleCenter;
            y -= 46;

            for (int i = 0; i < 5; i++)
            {
                var row = CreateImage(panel.transform, "DecorRow" + i, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(620, 56);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", GameManager.I.DecorSprite(i), Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(48, 48);
                icon.rectTransform.anchoredPosition = new Vector2(26, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", GameManager.DecorNames[i], 22, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(56, 0);
                name.rectTransform.offsetMax = new Vector2(-190, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var price = CreateText(row.transform, "Price", GameManager.DecorCosts[i] + " coins", 20, Palette.DeepPink);
                price.rectTransform.anchorMin = new Vector2(0, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(-185, 0);
                price.rectTransform.offsetMax = new Vector2(-130, 0);
                price.alignment = TextAnchor.MiddleLeft;

                int di = i;
                var buy = CreateButton(row.transform, "Buy", "Buy", 20, Color.white, out var buyBg);
                Rt(buy).anchorMin = new Vector2(1, 0);
                Rt(buy).anchorMax = new Vector2(1, 1);
                Rt(buy).pivot = new Vector2(1, 0.5f);
                Rt(buy).anchoredPosition = new Vector2(-10, 0);
                Rt(buy).sizeDelta = new Vector2(90, 40);
                buyBg.color = new Color(0.7f, 0.9f, 0.75f, 1);
                buyBg.sprite = RoundedSprite(Palette.Mint);
                buyBg.type = Image.Type.Sliced;
                buy.onClick.AddListener(() => { GameManager.I.BuyDecor(di); RefreshShop(); });
                y -= 64;
            }

            var decorBtn = CreateButton(panel.transform, "OpenDecor", "Place decor", 22, Color.white, out var decorBtnBg);
            Rt(decorBtn).anchorMin = new Vector2(0.5f, 1);
            Rt(decorBtn).anchorMax = new Vector2(0.5f, 1);
            Rt(decorBtn).pivot = new Vector2(0.5f, 1);
            Rt(decorBtn).anchoredPosition = new Vector2(0, y);
            Rt(decorBtn).sizeDelta = new Vector2(200, 48);
            decorBtnBg.color = new Color(0.85f, 0.78f, 0.95f, 1);
            decorBtnBg.sprite = RoundedSprite(Palette.Lavender);
            decorBtnBg.type = Image.Type.Sliced;
            decorBtn.onClick.AddListener(() => { OpenDecor(); shopPanel.SetActive(false); });
            y -= 56;

            // farm expansion
            var expandHeader = CreateText(panel.transform, "ExpandHeader", "~ Farm deeds ~", 28, Palette.Orange);
            expandHeader.rectTransform.anchorMin = new Vector2(0.5f, 1);
            expandHeader.rectTransform.anchorMax = new Vector2(0.5f, 1);
            expandHeader.rectTransform.pivot = new Vector2(0.5f, 1);
            expandHeader.rectTransform.anchoredPosition = new Vector2(0, y);
            expandHeader.rectTransform.sizeDelta = new Vector2(600, 40);
            expandHeader.alignment = TextAnchor.MiddleCenter;
            y -= 52;

            var expRow = CreateImage(panel.transform, "ExpansionRow", RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
            expRow.rectTransform.anchorMin = new Vector2(0.5f, 1);
            expRow.rectTransform.anchorMax = new Vector2(0.5f, 1);
            expRow.rectTransform.pivot = new Vector2(0.5f, 1);
            expRow.rectTransform.anchoredPosition = new Vector2(0, y);
            expRow.rectTransform.sizeDelta = new Vector2(620, 66);
            expRow.type = Image.Type.Sliced;

            var expIcon = CreateImage(expRow.transform, "Icon", SpriteBank.Deed, Color.white);
            expIcon.rectTransform.anchorMin = new Vector2(0, 0);
            expIcon.rectTransform.anchorMax = new Vector2(0, 1);
            expIcon.rectTransform.sizeDelta = new Vector2(58, 58);
            expIcon.rectTransform.anchoredPosition = new Vector2(32, 0);
            expIcon.preserveAspect = true;

            var expName = CreateText(expRow.transform, "Name", "Farm Expansion Deed", 24, Palette.Chocolate);
            expName.rectTransform.anchorMin = new Vector2(0, 0);
            expName.rectTransform.anchorMax = new Vector2(1, 1);
            expName.rectTransform.offsetMin = new Vector2(68, 0);
            expName.rectTransform.offsetMax = new Vector2(-190, 0);
            expName.alignment = TextAnchor.MiddleLeft;

            var expPrice = CreateText(expRow.transform, "Price", "", 22, Palette.Orange);
            expPrice.rectTransform.anchorMin = new Vector2(0, 0);
            expPrice.rectTransform.anchorMax = new Vector2(1, 1);
            expPrice.rectTransform.offsetMin = new Vector2(-185, 0);
            expPrice.rectTransform.offsetMax = new Vector2(-120, 0);
            expPrice.alignment = TextAnchor.MiddleLeft;

            var expBuy = CreateButton(expRow.transform, "Buy", "Buy", 24, Color.white, out var expBuyBg);
            Rt(expBuy).anchorMin = new Vector2(1, 0);
            Rt(expBuy).anchorMax = new Vector2(1, 1);
            Rt(expBuy).pivot = new Vector2(1, 0.5f);
            Rt(expBuy).anchoredPosition = new Vector2(-10, 0);
            Rt(expBuy).sizeDelta = new Vector2(100, 46);
            expBuyBg.color = new Color(0.9f, 0.75f, 0.6f, 1);
            expBuyBg.sprite = RoundedSprite(Palette.Peach);
            expBuyBg.type = Image.Type.Sliced;
            expBuy.onClick.AddListener(() =>
            {
                var farm = GameManager.I.Farm;
                int cost = farm.NextExpansionCost;
                if (GameManager.I.Money < cost)
                {
                    GameManager.I.Announce("Not enough coins for the deed~");
                    GameManager.I.Audio.Play(AudioService.Sfx.Nope);
                    return;
                }
                GameManager.I.Money -= cost;
                GameManager.I.OnMoneyChanged?.Invoke();
                farm.Expand();
                GameManager.I.Audio.Play(AudioService.Sfx.Harvest);
                GameManager.I.Announce("New land unlocked! Fresh soil awaits~");
                RefreshShop();
            });
            var expBuyText = expBuy.GetComponentInChildren<Text>();
            expBuyText.color = Color.white;
            y -= 66;

            // basket section grows upward from the panel bottom so it never overflows
            var basketHeader = CreateText(panel.transform, "BasketHeader", "~ Your basket ~", 28, Palette.BabyBlue);
            basketHeader.rectTransform.anchorMin = new Vector2(0.5f, 0);
            basketHeader.rectTransform.anchorMax = new Vector2(0.5f, 0);
            basketHeader.rectTransform.pivot = new Vector2(0.5f, 0);
            basketHeader.rectTransform.anchoredPosition = new Vector2(0, 175);
            basketHeader.rectTransform.sizeDelta = new Vector2(600, 36);
            basketHeader.alignment = TextAnchor.MiddleCenter;

            basketList = new GameObject("BasketList").AddComponent<RectTransform>();
            basketList.SetParent(panel.transform, false);
            basketList.anchorMin = new Vector2(0.5f, 0);
            basketList.anchorMax = new Vector2(0.5f, 0);
            basketList.pivot = new Vector2(0.5f, 1);
            basketList.anchoredPosition = new Vector2(0, 130);
            basketList.sizeDelta = new Vector2(640, 6 * 56);

            var sellAll = CreateButton(panel.transform, "SellAll", "Sell all", 24, Color.white, out var sellAllBg);
            Rt(sellAll).anchorMin = new Vector2(0.5f, 0);
            Rt(sellAll).anchorMax = new Vector2(0.5f, 0);
            Rt(sellAll).pivot = new Vector2(0.5f, 0);
            Rt(sellAll).anchoredPosition = new Vector2(0, 22);
            Rt(sellAll).sizeDelta = new Vector2(220, 54);
            sellAllBg.color = new Color(0.9f, 0.75f, 0.6f, 1);
            sellAllBg.sprite = RoundedSprite(Palette.Peach);
            sellAllBg.type = Image.Type.Sliced;
            sellAll.onClick.AddListener(() => { GameManager.I.SellAllInBasket(); RefreshShop(); });

            shopPanel.SetActive(false);
        }

        void BuildBasketRows()
        {
            foreach (var r in shopRows) Destroy(r);
            shopRows.Clear();
            float y = 0;
            for (int i = 0; i < 6; i++)
            {
                int count = GameManager.I.Basket[i];
                if (count <= 0) continue;
                var def = CropDef.All[i];
                var row = CreateImage(basketList, "BasketRow" + i, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(600, 60);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", SpriteBank.CropSprites[i, 3], Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(52, 52);
                icon.rectTransform.anchoredPosition = new Vector2(30, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", $"{def.Name} x{count}", 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(64, 0);
                name.rectTransform.offsetMax = new Vector2(-180, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var price = CreateText(row.transform, "Price", (def.CurrentSellPrice(GameManager.I.CurrentSeason) * count) + " coins" + (def.Season == GameManager.I.CurrentSeason ? "  ★" : ""), 22, Palette.Chocolate);
                price.rectTransform.anchorMin = new Vector2(0, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(-175, 0);
                price.rectTransform.offsetMax = new Vector2(-110, 0);
                price.alignment = TextAnchor.MiddleLeft;

                var sell = CreateButton(row.transform, "Sell", "Sell", 22, Color.white, out var sellBg);
                Rt(sell).anchorMin = new Vector2(1, 0);
                Rt(sell).anchorMax = new Vector2(1, 1);
                Rt(sell).pivot = new Vector2(1, 0.5f);
                Rt(sell).anchoredPosition = new Vector2(-10, 0);
                Rt(sell).sizeDelta = new Vector2(90, 42);
                sellBg.sprite = RoundedSprite(Palette.Peach);
                sellBg.type = Image.Type.Sliced;
                CropId crop = (CropId)i;
                sell.onClick.AddListener(() =>
                {
                    var gm = GameManager.I;
                    int value = gm.Basket[(int)crop] * def.CurrentSellPrice(gm.CurrentSeason);
                    gm.Basket[(int)crop] = 0;
                    gm.Money += value;
                    gm.Announce($"Sold {def.Name} for {value} coins!");
                    gm.Audio.Play(AudioService.Sfx.Coin);
                    gm.OnMoneyChanged?.Invoke();
                    gm.OnBasketChanged?.Invoke();
                    RefreshShop();
                });
                shopRows.Add(row.gameObject);
                y -= 66;
            }
            // fish rows
            float fy = y;
            for (int i = 0; i < 3; i++)
            {
                int count = GameManager.I.FishBasket[i];
                if (count <= 0) continue;
                var def = FishDef.All[i];
                var row = CreateImage(basketList, "FishRow" + i, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, fy);
                row.rectTransform.sizeDelta = new Vector2(600, 60);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", i == 0 ? SpriteBank.Goldfish : i == 1 ? SpriteBank.BubbleFish : SpriteBank.SakuraFish, Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(52, 52);
                icon.rectTransform.anchoredPosition = new Vector2(30, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", $"{def.Name} x{count}", 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(64, 0);
                name.rectTransform.offsetMax = new Vector2(-180, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var price = CreateText(row.transform, "Price", (def.SellPrice * count) + " coins", 22, Palette.Chocolate);
                price.rectTransform.anchorMin = new Vector2(0, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(-175, 0);
                price.rectTransform.offsetMax = new Vector2(-110, 0);
                price.alignment = TextAnchor.MiddleLeft;

                var sell = CreateButton(row.transform, "Sell", "Sell", 22, Color.white, out var sellBg);
                Rt(sell).anchorMin = new Vector2(1, 0);
                Rt(sell).anchorMax = new Vector2(1, 1);
                Rt(sell).pivot = new Vector2(1, 0.5f);
                Rt(sell).anchoredPosition = new Vector2(-10, 0);
                Rt(sell).sizeDelta = new Vector2(90, 42);
                sellBg.color = new Color(0.9f, 0.75f, 0.6f, 1);
                sellBg.sprite = RoundedSprite(Palette.Peach);
                sellBg.type = Image.Type.Sliced;
                int fid = i;
                sell.onClick.AddListener(() =>
                {
                    var gm = GameManager.I;
                    int value = gm.FishBasket[fid] * FishDef.All[fid].SellPrice;
                    gm.FishBasket[fid] = 0;
                    gm.Money += value;
                    gm.Announce($"Sold {FishDef.All[fid].Name} for {value} coins!");
                    gm.Audio.Play(AudioService.Sfx.Coin);
                    gm.OnMoneyChanged?.Invoke();
                    gm.OnBasketChanged?.Invoke();
                    RefreshShop();
                });
                shopRows.Add(row.gameObject);
                fy -= 66;
            }

            // egg row
            if (GameManager.I.EggBasket > 0)
            {
                var row = CreateImage(basketList, "EggRow", RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, fy);
                row.rectTransform.sizeDelta = new Vector2(600, 60);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", SpriteBank.Egg, Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(52, 52);
                icon.rectTransform.anchoredPosition = new Vector2(30, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", $"Egg x{GameManager.I.EggBasket}", 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(64, 0);
                name.rectTransform.offsetMax = new Vector2(-320, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var price = CreateText(row.transform, "Price", (GameManager.I.EggBasket * GameManager.EggSellPrice) + " coins", 22, Palette.Chocolate);
                price.rectTransform.anchorMin = new Vector2(0, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(-315, 0);
                price.rectTransform.offsetMax = new Vector2(-250, 0);
                price.alignment = TextAnchor.MiddleLeft;

                var eat = CreateButton(row.transform, "Eat", "Eat", 20, Color.white, out var eatBg);
                Rt(eat).anchorMin = new Vector2(1, 0);
                Rt(eat).anchorMax = new Vector2(1, 1);
                Rt(eat).pivot = new Vector2(1, 0.5f);
                Rt(eat).anchoredPosition = new Vector2(-100, 0);
                Rt(eat).sizeDelta = new Vector2(80, 42);
                eatBg.color = new Color(0.95f, 0.85f, 0.7f, 1);
                eatBg.sprite = RoundedSprite(Palette.Peach);
                eatBg.type = Image.Type.Sliced;
                eat.onClick.AddListener(() => { GameManager.I.EatEgg(); RefreshShop(); });

                var sell = CreateButton(row.transform, "Sell", "Sell", 22, Color.white, out var sellBg2);
                Rt(sell).anchorMin = new Vector2(1, 0);
                Rt(sell).anchorMax = new Vector2(1, 1);
                Rt(sell).pivot = new Vector2(1, 0.5f);
                Rt(sell).anchoredPosition = new Vector2(-10, 0);
                Rt(sell).sizeDelta = new Vector2(90, 42);
                sellBg2.color = new Color(0.9f, 0.75f, 0.6f, 1);
                sellBg2.sprite = RoundedSprite(Palette.Peach);
                sellBg2.type = Image.Type.Sliced;
                sell.onClick.AddListener(() =>
                {
                    var gm = GameManager.I;
                    int value = gm.EggBasket * GameManager.EggSellPrice;
                    gm.EggBasket = 0;
                    gm.Money += value;
                    gm.Announce($"Sold eggs for {value} coins!");
                    gm.Audio.Play(AudioService.Sfx.Coin);
                    gm.OnMoneyChanged?.Invoke();
                    gm.OnBasketChanged?.Invoke();
                    RefreshShop();
                });
                shopRows.Add(row.gameObject);
            }

            // honey row
            if (GameManager.I.HoneyBasket > 0)
            {
                var row = CreateImage(basketList, "HoneyRow", RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, fy);
                row.rectTransform.sizeDelta = new Vector2(600, 60);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", SpriteBank.Honey, Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(52, 52);
                icon.rectTransform.anchoredPosition = new Vector2(30, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", $"Honey x{GameManager.I.HoneyBasket}", 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(64, 0);
                name.rectTransform.offsetMax = new Vector2(-320, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var price = CreateText(row.transform, "Price", (GameManager.I.HoneyBasket * GameManager.HoneySellPrice) + " coins", 22, Palette.Chocolate);
                price.rectTransform.anchorMin = new Vector2(0, 0);
                price.rectTransform.anchorMax = new Vector2(1, 1);
                price.rectTransform.offsetMin = new Vector2(-315, 0);
                price.rectTransform.offsetMax = new Vector2(-250, 0);
                price.alignment = TextAnchor.MiddleLeft;

                var eat = CreateButton(row.transform, "Eat", "Eat", 20, Color.white, out var eatBg2);
                Rt(eat).anchorMin = new Vector2(1, 0);
                Rt(eat).anchorMax = new Vector2(1, 1);
                Rt(eat).pivot = new Vector2(1, 0.5f);
                Rt(eat).anchoredPosition = new Vector2(-100, 0);
                Rt(eat).sizeDelta = new Vector2(80, 42);
                eatBg2.color = new Color(0.95f, 0.85f, 0.7f, 1);
                eatBg2.sprite = RoundedSprite(Palette.Peach);
                eatBg2.type = Image.Type.Sliced;
                eat.onClick.AddListener(() => { GameManager.I.EatHoney(); RefreshShop(); });

                var sell = CreateButton(row.transform, "Sell", "Sell", 22, Color.white, out var sellBg3);
                Rt(sell).anchorMin = new Vector2(1, 0);
                Rt(sell).anchorMax = new Vector2(1, 1);
                Rt(sell).pivot = new Vector2(1, 0.5f);
                Rt(sell).anchoredPosition = new Vector2(-10, 0);
                Rt(sell).sizeDelta = new Vector2(90, 42);
                sellBg3.color = new Color(0.9f, 0.75f, 0.6f, 1);
                sellBg3.sprite = RoundedSprite(Palette.Peach);
                sellBg3.type = Image.Type.Sliced;
                sell.onClick.AddListener(() =>
                {
                    var gm = GameManager.I;
                    int value = gm.HoneyBasket * GameManager.HoneySellPrice;
                    gm.HoneyBasket = 0;
                    gm.Money += value;
                    gm.Announce($"Sold honey for {value} coins!");
                    gm.Audio.Play(AudioService.Sfx.Coin);
                    gm.OnMoneyChanged?.Invoke();
                    gm.OnBasketChanged?.Invoke();
                    RefreshShop();
                });
                shopRows.Add(row.gameObject);
            }

            // hide basket list if empty
            basketList.gameObject.SetActive(GameManager.I.BasketTotalValue() > 0 || GameManager.I.FishBasketTotalValue() > 0 || GameManager.I.EggBasket > 0 || GameManager.I.HoneyBasket > 0);
        }

        public void OpenShop(NpcController mochi)
        {
            if (GameManager.I.IsNight)
            {
                GameManager.I.Announce("Mochi is snoozing... Zzz");
                return;
            }
            RefreshShop();
            shopPanel.SetActive(true);
            GameManager.I.Audio.Play(AudioService.Sfx.Meow);
            ShowBubble(mochi.transform.position + new Vector3(0, 0.6f, 0), "Meow! Welcome to Mochi's Market!");
            mochi.GetComponent<SpriteRenderer>().flipX = false;
        }

        void RefreshShop()
        {
            if (shopCoins == null) return;
            shopCoins.text = $"{GameManager.I.Money} coins";
            var farm = GameManager.I.Farm;
            var expRow = shopPanel.transform.Find("Panel/ExpansionRow");
            if (expRow != null)
            {
                var priceT = expRow.Find("Price").GetComponent<Text>();
                var buyB = expRow.Find("Buy").GetComponent<Button>();
                if (farm.CanExpand)
                {
                    expRow.gameObject.SetActive(true);
                    priceT.text = farm.NextExpansionCost + " coins";
                    buyB.interactable = GameManager.I.Money >= farm.NextExpansionCost;
                }
                else
                {
                    expRow.gameObject.SetActive(true);
                    priceT.text = "fully expanded!";
                    buyB.interactable = false;
                }
            }
            BuildBasketRows();
        }

        void BuildFishing()
        {
            fishPanel = new GameObject("FishingPanel");
            fishPanel.transform.SetParent(Root, false);
            var fRt = fishPanel.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(fishPanel.transform, "Backdrop", null, new Color(0.12f, 0.18f, 0.30f, 0.6f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(fishPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(520, 480);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Fishing!", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -20);
            title.rectTransform.sizeDelta = new Vector2(400, 44);
            title.alignment = TextAnchor.MiddleCenter;

            var hint = CreateText(panel.transform, "Hint", "tap when the bobber is on the fish!", 20, Palette.DeepPink);
            hint.rectTransform.anchorMin = new Vector2(0.5f, 1);
            hint.rectTransform.anchorMax = new Vector2(0.5f, 1);
            hint.rectTransform.pivot = new Vector2(0.5f, 1);
            hint.rectTransform.anchoredPosition = new Vector2(0, -62);
            hint.rectTransform.sizeDelta = new Vector2(400, 28);
            hint.alignment = TextAnchor.MiddleCenter;

            fishTimer = CreateText(panel.transform, "Timer", "6", 26, Palette.Chocolate);
            fishTimer.rectTransform.anchorMin = new Vector2(0.5f, 1);
            fishTimer.rectTransform.anchorMax = new Vector2(0.5f, 1);
            fishTimer.rectTransform.pivot = new Vector2(0.5f, 1);
            fishTimer.rectTransform.anchoredPosition = new Vector2(0, -96);
            fishTimer.rectTransform.sizeDelta = new Vector2(80, 30);
            fishTimer.alignment = TextAnchor.MiddleCenter;

            // water column
            var water = CreateImage(panel.transform, "Water", RoundedSprite(Palette.BabyBlue), new Color(0.55f, 0.75f, 0.95f, 0.9f));
            water.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            water.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            water.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            water.rectTransform.anchoredPosition = new Vector2(0, -10);
            water.rectTransform.sizeDelta = new Vector2(140, 320);
            water.type = Image.Type.Sliced;

            // fish zone
            var zoneGo = CreateImage(water.transform, "Zone", RoundedSprite(Palette.PetalPink), new Color(1f, 0.72f, 0.82f, 0.75f));
            fishZone = zoneGo.rectTransform;
            fishZoneImg = zoneGo;
            fishZone.anchorMin = new Vector2(0.5f, 0.5f);
            fishZone.anchorMax = new Vector2(0.5f, 0.5f);
            fishZone.pivot = new Vector2(0.5f, 0.5f);
            fishZone.sizeDelta = new Vector2(110, 64);

            // cursor (bobber): red & white ball so it pops against the water
            var cursorGo = CreateImage(water.transform, "Cursor", SpriteFactory.SoftCircle(32, Color.white), Color.white);
            fishCursor = cursorGo.rectTransform;
            fishCursor.anchorMin = new Vector2(0.5f, 0.5f);
            fishCursor.anchorMax = new Vector2(0.5f, 0.5f);
            fishCursor.pivot = new Vector2(0.5f, 0.5f);
            fishCursor.sizeDelta = new Vector2(34, 34);
            var cursorImg = cursorGo;
            cursorImg.color = new Color(0.95f, 0.9f, 0.8f, 1f);
            var bobberStripe = CreateImage(water.transform, "Stripe", SpriteFactory.SoftCircle(32, Color.white), new Color(0.92f, 0.35f, 0.45f, 1f));
            bobberStripe.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bobberStripe.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bobberStripe.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bobberStripe.rectTransform.sizeDelta = new Vector2(34, 16);
            bobberStripe.rectTransform.SetParent(fishCursor, false);
            bobberStripe.rectTransform.anchoredPosition = new Vector2(0, -8);

            fishPanel.SetActive(false);
        }

        public void OpenFishing()
        {
            if (fishingActive || GameManager.I == null) return;
            if (!GameManager.I.SpendEnergy(3)) return;
            GameManager.I.Audio.Play(AudioService.Sfx.Splash);
            fishingActive = true;
            fishingDone = false;
            fishingT = 0f;
            fishZoneImg.sprite = RoundedSprite(Palette.PetalPink);
            fishZoneImg.color = new Color(1f, 0.72f, 0.82f, 0.75f);
            fishZone.sizeDelta = new Vector2(110, 64);
            fishZone.anchoredPosition = Vector2.zero;
            fishZone.gameObject.SetActive(true);
            fishPanel.SetActive(true);
            PositionFishing();
        }

        void PositionFishing()
        {
            float zoneC = 0.5f + 0.30f * Mathf.Sin(fishingT * 1.4f) + 0.08f * Mathf.Sin(fishingT * 3.7f);
            fishZone.anchoredPosition = new Vector2(0, Mathf.Lerp(-140f, 140f, zoneC));
            float cursorF = 0.5f + 0.44f * Mathf.Sin(fishingT * 2.1f);
            fishCursor.anchoredPosition = new Vector2(0, Mathf.Lerp(-140f, 140f, cursorF));
            if (fishTimer != null) fishTimer.text = Mathf.Max(0, Mathf.CeilToInt(6f - fishingT)).ToString();
        }

        void UpdateFishing()
        {
            if (!fishingActive) return;
            fishingT += Time.deltaTime;
            PositionFishing();

            if (fishingDone) return;

            var input = InputService.I;
            bool tapped = (input != null && input.ActionPressed) || fishingTapQueued;
            fishingTapQueued = false;

            if (tapped)
            {
                float zoneC = 0.5f + 0.30f * Mathf.Sin(fishingT * 1.4f) + 0.08f * Mathf.Sin(fishingT * 3.7f);
                float cursorF = 0.5f + 0.44f * Mathf.Sin(fishingT * 2.1f);
                if (Mathf.Abs(cursorF - zoneC) < 0.13f)
                {
                    CatchFish();
                    return;
                }
                GameManager.I.Audio.Play(AudioService.Sfx.Tap);
            }

            if (fishingT >= 6f)
            {
                fishingDone = true;
                GameManager.I.Audio.Play(AudioService.Sfx.Nope);
                GameManager.I.Announce("The fish got away... try again!");
                fishZone.gameObject.SetActive(false);
                StartCoroutine(CloseFishing(1.2f));
            }
        }

        void CatchFish()
        {
            if (fishingDone) return;
            fishingDone = true;
            var gm = GameManager.I;
            int roll = Random.Range(0, 100);
            int fishId = roll < 50 ? 0 : roll < 82 ? 1 : 2;
            gm.AddFish(fishId, 1);
            gm.AddEnergy(2f);
            gm.Audio.Play(AudioService.Sfx.Harvest);
            gm.Announce($"Caught a {FishDef.All[fishId].Name}! So shiny~");
            fishZone.gameObject.SetActive(true);
            var sprite = fishId == 0 ? SpriteBank.Goldfish : fishId == 1 ? SpriteBank.BubbleFish : SpriteBank.SakuraFish;
            var img = fishZone.GetComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.rectTransform.sizeDelta = new Vector2(96, 60);
            img.rectTransform.anchoredPosition = new Vector2(0, 0);
            StartCoroutine(CloseFishing(1.4f));
        }

        System.Collections.IEnumerator CloseFishing(float delay)
        {
            yield return new WaitForSeconds(delay);
            fishingActive = false;
            fishPanel.SetActive(false);
            fishingTapQueued = false;
        }

        void BuildCooking()
        {
            cookPanel = new GameObject("CookPanel");
            cookPanel.transform.SetParent(Root, false);
            var cRt = cookPanel.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(cookPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(cookPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(620, 560);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Cozy Kitchen", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(500, 46);
            title.alignment = TextAnchor.MiddleCenter;

            var sub = CreateText(panel.transform, "Sub", "cook crops into energy snacks", 20, Palette.DeepPink);
            sub.rectTransform.anchorMin = new Vector2(0.5f, 1);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 1);
            sub.rectTransform.pivot = new Vector2(0.5f, 1);
            sub.rectTransform.anchoredPosition = new Vector2(0, -66);
            sub.rectTransform.sizeDelta = new Vector2(500, 28);
            sub.alignment = TextAnchor.MiddleCenter;

            cookList = new GameObject("CookList").AddComponent<RectTransform>();
            cookList.SetParent(panel.transform, false);
            cookList.anchorMin = new Vector2(0.5f, 1);
            cookList.anchorMax = new Vector2(0.5f, 1);
            cookList.pivot = new Vector2(0.5f, 1);
            cookList.anchoredPosition = new Vector2(0, -110);
            cookList.sizeDelta = new Vector2(560, 4 * 100);

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => cookPanel.SetActive(false));

            cookPanel.SetActive(false);
        }

        void BuildCookRows()
        {
            foreach (var r in cookRows) Destroy(r);
            cookRows.Clear();
            var gm = GameManager.I;
            if (gm == null) return;
            float y = 0;
            foreach (var r in GameManager.Recipe.All)
            {
                bool can = gm.CanCook(r);
                var row = CreateImage(cookList, "CookRow" + r.Name, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.75f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(540, 88);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", SpriteBank.CropSprites[r.CropId, 3], Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(56, 56);
                icon.rectTransform.anchoredPosition = new Vector2(32, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", r.Name, 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(66, 22);
                name.rectTransform.offsetMax = new Vector2(-190, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var need = CreateText(row.transform, "Need", r.Desc + "  ·  +" + r.Energy + " energy", 19, Palette.DeepPink);
                need.rectTransform.anchorMin = new Vector2(0, 0);
                need.rectTransform.anchorMax = new Vector2(1, 0);
                need.rectTransform.offsetMin = new Vector2(66, 8);
                need.rectTransform.offsetMax = new Vector2(-190, 34);
                need.alignment = TextAnchor.MiddleLeft;

                var cook = CreateButton(row.transform, "Cook", "Cook", 22, Color.white, out var cookBg);
                Rt(cook).anchorMin = new Vector2(1, 0);
                Rt(cook).anchorMax = new Vector2(1, 1);
                Rt(cook).pivot = new Vector2(1, 0.5f);
                Rt(cook).anchoredPosition = new Vector2(-12, 0);
                Rt(cook).sizeDelta = new Vector2(110, 54);
                cookBg.color = can ? new Color(0.75f, 0.9f, 0.75f, 1) : new Color(0.85f, 0.8f, 0.85f, 0.8f);
                cookBg.sprite = RoundedSprite(Palette.Mint);
                cookBg.type = Image.Type.Sliced;
                cook.interactable = can;
                var captured = r;
                cook.onClick.AddListener(() =>
                {
                    GameManager.I.Cook(captured);
                    RefreshCook();
                });
                cookRows.Add(row.gameObject);
                y -= 100;
            }
        }

        public void OpenCooking()
        {
            RefreshCook();
            cookPanel.SetActive(true);
        }

        void RefreshCook()
        {
            if (cookList == null) return;
            BuildCookRows();
        }

        void BuildDecor()
        {
            decorPanel = new GameObject("DecorPanel");
            decorPanel.transform.SetParent(Root, false);
            var dRt = decorPanel.AddComponent<RectTransform>();
            dRt.anchorMin = Vector2.zero; dRt.anchorMax = Vector2.one;
            dRt.offsetMin = Vector2.zero; dRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(decorPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(decorPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(620, 520);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Farm Decor", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(500, 46);
            title.alignment = TextAnchor.MiddleCenter;

            var sub = CreateText(panel.transform, "Sub", "place decor on the meadow · buy more in Mochi's shop", 20, Palette.DeepPink);
            sub.rectTransform.anchorMin = new Vector2(0.5f, 1);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 1);
            sub.rectTransform.pivot = new Vector2(0.5f, 1);
            sub.rectTransform.anchoredPosition = new Vector2(0, -64);
            sub.rectTransform.sizeDelta = new Vector2(500, 28);
            sub.alignment = TextAnchor.MiddleCenter;

            decorList = new GameObject("DecorList").AddComponent<RectTransform>();
            decorList.SetParent(panel.transform, false);
            decorList.anchorMin = new Vector2(0.5f, 1);
            decorList.anchorMax = new Vector2(0.5f, 1);
            decorList.pivot = new Vector2(0.5f, 1);
            decorList.anchoredPosition = new Vector2(0, -110);
            decorList.sizeDelta = new Vector2(560, 4 * 96);

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() =>
            {
                decorPanel.SetActive(false);
                GameManager.I.DecorPlacing = -1;
            });

            decorPanel.SetActive(false);
        }

        void BuildDecorRows()
        {
            foreach (var r in decorRows) Destroy(r);
            decorRows.Clear();
            var gm = GameManager.I;
            if (gm == null) return;
            float y = 0;
            for (int i = 0; i < GameManager.DecorNames.Length; i++)
            {
                int count = gm.DecorInventory[i];
                var row = CreateImage(decorList, "DecorRow" + i, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.75f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(540, 84);
                row.type = Image.Type.Sliced;

                var icon = CreateImage(row.transform, "Icon", gm.DecorSprite(i), Color.white);
                icon.rectTransform.anchorMin = new Vector2(0, 0);
                icon.rectTransform.anchorMax = new Vector2(0, 1);
                icon.rectTransform.sizeDelta = new Vector2(56, 56);
                icon.rectTransform.anchoredPosition = new Vector2(32, 0);
                icon.preserveAspect = true;

                var name = CreateText(row.transform, "Name", $"{GameManager.DecorNames[i]}  x{count}", 24, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(66, 0);
                name.rectTransform.offsetMax = new Vector2(-180, 0);
                name.alignment = TextAnchor.MiddleLeft;

                int idx = i;
                var place = CreateButton(row.transform, "Place", "Place", 22, Color.white, out var placeBg);
                Rt(place).anchorMin = new Vector2(1, 0);
                Rt(place).anchorMax = new Vector2(1, 1);
                Rt(place).pivot = new Vector2(1, 0.5f);
                Rt(place).anchoredPosition = new Vector2(-12, 0);
                Rt(place).sizeDelta = new Vector2(110, 54);
                placeBg.color = count > 0 ? new Color(0.8f, 0.88f, 0.98f, 1) : new Color(0.85f, 0.8f, 0.85f, 0.7f);
                placeBg.sprite = RoundedSprite(Palette.BabyBlue);
                placeBg.type = Image.Type.Sliced;
                place.interactable = count > 0;
                place.onClick.AddListener(() =>
                {
                    GameManager.I.StartPlacing(idx);
                    decorPanel.SetActive(false);
                });
                decorRows.Add(row.gameObject);
                y -= 96;
            }
        }

        public void OpenDecor()
        {
            RefreshDecor();
            decorPanel.SetActive(true);
        }

        void RefreshDecor()
        {
            if (decorList == null) return;
            BuildDecorRows();
        }

        void BuildScrapbook()
        {
            scrapPanel = new GameObject("ScrapPanel");
            scrapPanel.transform.SetParent(Root, false);
            var sRt = scrapPanel.AddComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
            sRt.offsetMin = Vector2.zero; sRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(scrapPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(scrapPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(640, 720);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Mochi's Scrapbook", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(500, 46);
            title.alignment = TextAnchor.MiddleCenter;

            scrapList = new GameObject("ScrapList").AddComponent<RectTransform>();
            scrapList.SetParent(panel.transform, false);
            scrapList.anchorMin = new Vector2(0.5f, 1);
            scrapList.anchorMax = new Vector2(0.5f, 1);
            scrapList.pivot = new Vector2(0.5f, 1);
            scrapList.anchoredPosition = new Vector2(0, -80);
            scrapList.sizeDelta = new Vector2(580, 620);

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => scrapPanel.SetActive(false));

            scrapPanel.SetActive(false);
        }

        void BuildScrapRows()
        {
            foreach (var r in scrapRows) Destroy(r);
            scrapRows.Clear();
            var gm = GameManager.I;
            if (gm == null) return;
            float y = 0;

            AddScrapHeader("Crops", ref y);
            for (int i = 0; i < 6; i++)
            {
                bool got = (gm.CollectedCropsMask & (1 << i)) != 0;
                AddScrapRow(SpriteBank.CropSprites[i, 3], CropDef.All[i].Name, got, ref y);
            }
            AddScrapHeader("Fish", ref y);
            for (int i = 0; i < 3; i++)
            {
                bool got = (gm.CollectedFishMask & (1 << i)) != 0;
                AddScrapRow(i == 0 ? SpriteBank.Goldfish : i == 1 ? SpriteBank.BubbleFish : SpriteBank.SakuraFish, FishDef.All[i].Name, got, ref y);
            }
            AddScrapHeader("Delicacies", ref y);
            AddScrapRow(SpriteBank.Egg, "Eggs", gm.CollectedEgg, ref y);
            AddScrapRow(SpriteBank.Honey, "Honey", gm.CollectedHoney, ref y);

            AddScrapHeader("Memories", ref y);
            int collected = CountBits(gm.CollectedCropsMask) + CountBits(gm.CollectedFishMask) + (gm.CollectedEgg ? 1 : 0) + (gm.CollectedHoney ? 1 : 0);
            AddScrapRow(null, $"Collected {collected} of 12", false, ref y, countAsText: $"{collected}/12");
            AddScrapRow(null, "Coins earned in total", false, ref y, countAsText: gm.TotalEarned.ToString());
            AddScrapRow(null, "Days on the farm", false, ref y, countAsText: gm.Day.ToString());
        }

        static int CountBits(int mask)
        {
            int n = 0;
            while (mask != 0) { n += mask & 1; mask >>= 1; }
            return n;
        }

        void AddScrapHeader(string text, ref float y)
        {
            var h = CreateText(scrapList, "H" + text, "~ " + text + " ~", 22, Palette.DeepPink);
            h.rectTransform.anchorMin = new Vector2(0.5f, 1);
            h.rectTransform.anchorMax = new Vector2(0.5f, 1);
            h.rectTransform.pivot = new Vector2(0.5f, 1);
            h.rectTransform.anchoredPosition = new Vector2(0, y);
            h.rectTransform.sizeDelta = new Vector2(500, 30);
            h.alignment = TextAnchor.MiddleCenter;
            scrapRows.Add(h.gameObject);
            y -= 34;
        }

        void AddScrapRow(Sprite icon, string name, bool got, ref float y, string countAsText = "")
        {
            var row = CreateImage(scrapList, "ScrapRow" + name, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
            row.rectTransform.anchorMin = new Vector2(0.5f, 1);
            row.rectTransform.anchorMax = new Vector2(0.5f, 1);
            row.rectTransform.pivot = new Vector2(0.5f, 1);
            row.rectTransform.anchoredPosition = new Vector2(0, y);
            row.rectTransform.sizeDelta = new Vector2(520, 48);
            row.type = Image.Type.Sliced;

            if (icon != null)
            {
                var ic = CreateImage(row.transform, "Icon", icon, Color.white);
                ic.rectTransform.anchorMin = new Vector2(0, 0);
                ic.rectTransform.anchorMax = new Vector2(0, 1);
                ic.rectTransform.sizeDelta = new Vector2(42, 42);
                ic.rectTransform.anchoredPosition = new Vector2(24, 0);
                ic.preserveAspect = true;
                ic.color = got ? Color.white : new Color(0.4f, 0.35f, 0.45f, 0.35f);
            }

            var nameText = CreateText(row.transform, "Name", got ? name : "???", 22, got ? Palette.Chocolate : new Color(0.5f, 0.45f, 0.55f, 0.8f));
            nameText.rectTransform.anchorMin = new Vector2(0, 0);
            nameText.rectTransform.anchorMax = new Vector2(1, 1);
            nameText.rectTransform.offsetMin = new Vector2(52, 0);
            nameText.rectTransform.offsetMax = new Vector2(-120, 0);
            nameText.alignment = TextAnchor.MiddleLeft;

            var mark = CreateText(row.transform, "Mark", got ? "✓" : "?", 24, got ? Palette.LeafDark : new Color(0.5f, 0.45f, 0.55f, 0.8f));
            mark.rectTransform.anchorMin = new Vector2(1, 0);
            mark.rectTransform.anchorMax = new Vector2(1, 1);
            mark.rectTransform.offsetMin = new Vector2(-110, 0);
            mark.rectTransform.offsetMax = new Vector2(-20, 0);
            mark.alignment = TextAnchor.MiddleRight;

            if (countAsText != "")
            {
                mark.text = countAsText;
                mark.fontSize = 18;
            }
            scrapRows.Add(row.gameObject);
            y -= 54;
        }

        public void OpenScrapbook()
        {
            BuildScrapRows();
            scrapPanel.SetActive(true);
        }

        void BuildMakeover()
        {
            makeoverPanel = new GameObject("MakeoverPanel");
            makeoverPanel.transform.SetParent(Root, false);
            var mRt = makeoverPanel.AddComponent<RectTransform>();
            mRt.anchorMin = Vector2.zero; mRt.anchorMax = Vector2.one;
            mRt.offsetMin = Vector2.zero; mRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(makeoverPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(makeoverPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(660, 420);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Mochi's Makeover", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(600, 46);
            title.alignment = TextAnchor.MiddleCenter;

            var sub = CreateText(panel.transform, "Sub", "pick a look · new colors cost coins, owned colors are free", 20, Palette.DeepPink);
            sub.rectTransform.anchorMin = new Vector2(0.5f, 1);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 1);
            sub.rectTransform.pivot = new Vector2(0.5f, 1);
            sub.rectTransform.anchoredPosition = new Vector2(0, -66);
            sub.rectTransform.sizeDelta = new Vector2(600, 30);
            sub.alignment = TextAnchor.MiddleCenter;

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => makeoverPanel.SetActive(false));

            makeoverPanel.SetActive(false);
        }

        void BuildMakeoverRows()
        {
            foreach (var r in makeoverRows) Destroy(r);
            makeoverRows.Clear();
            var gm = GameManager.I;
            if (gm == null) return;

            var panel = makeoverPanel.transform.Find("Panel");
            BuildSwatchRow(panel, "Dress", GameManager.DressNames, GameManager.DressColors, gm.OutfitDress, GameManager.DressCost,
                i => gm.OwnsDress(i), i => gm.BuyDress(i), new Vector2(0, -120));
            BuildSwatchRow(panel, "Hair", GameManager.HairNames, GameManager.HairColors, gm.OutfitHair, GameManager.HairCost,
                i => gm.OwnsHair(i), i => gm.BuyHair(i), new Vector2(0, -230));
        }

        void BuildSwatchRow(Transform panel, string label, string[] names, Color[] colors, int current,
            int cost, System.Func<int, bool> owns, System.Action<int> buy, Vector2 pos)
        {
            var row = new GameObject(label + "Row").AddComponent<RectTransform>();
            row.SetParent(panel, false);
            row.anchorMin = new Vector2(0.5f, 1);
            row.anchorMax = new Vector2(0.5f, 1);
            row.pivot = new Vector2(0.5f, 1);
            row.anchoredPosition = pos;
            row.sizeDelta = new Vector2(600, 80);
            makeoverRows.Add(row.gameObject);

            var labelText = CreateText(row, "Label", label, 26, Palette.Chocolate);
            labelText.rectTransform.anchorMin = new Vector2(0, 0);
            labelText.rectTransform.anchorMax = new Vector2(0, 1);
            labelText.rectTransform.sizeDelta = new Vector2(90, 40);
            labelText.rectTransform.anchoredPosition = new Vector2(45, 0);
            labelText.alignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < colors.Length; i++)
            {
                int idx = i;
                bool owned = owns(i);
                bool active = i == current;
                var swatch = CreateButton(row, "Sw" + i, "", 14, Color.white, out var swatchBg);
                Rt(swatch).anchorMin = new Vector2(0, 0);
                Rt(swatch).anchorMax = new Vector2(0, 1);
                Rt(swatch).pivot = new Vector2(0, 0.5f);
                Rt(swatch).anchoredPosition = new Vector2(100 + i * 92f, 0);
                Rt(swatch).sizeDelta = new Vector2(80, 56);
                swatchBg.color = colors[i];
                swatchBg.sprite = RoundedSprite(colors[i]);
                swatchBg.type = Image.Type.Sliced;
                var labelGo = swatch.GetComponentInChildren<Text>();
                labelGo.text = active ? "★" : (owned ? "✓" : cost + "");
                labelGo.fontSize = 16;
                labelGo.color = Color.white;
                AddOutline(labelGo, 1f, new Color(0.3f, 0.24f, 0.35f, 0.9f));
                swatch.onClick.AddListener(() =>
                {
                    buy(idx);
                    RefreshMakeover();
                });
            }
        }

        public void OpenMakeover()
        {
            RefreshMakeover();
            makeoverPanel.SetActive(true);
        }

        public void RefreshMakeover()
        {
            if (makeoverPanel == null) return;
            BuildMakeoverRows();
        }

        void BuildQuestPanel()
        {
            questPanel = new GameObject("QuestPanel");
            questPanel.transform.SetParent(Root, false);
            var questRt = questPanel.AddComponent<RectTransform>();
            questRt.anchorMin = Vector2.zero; questRt.anchorMax = Vector2.one;
            questRt.offsetMin = Vector2.zero; questRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(questPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(questPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(660, 520);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Mochi's Quest Board", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(600, 46);
            title.alignment = TextAnchor.MiddleCenter;

            var sub = CreateText(panel.transform, "Sub", "daily cozy tasks from Mochi", 22, Palette.DeepPink);
            sub.rectTransform.anchorMin = new Vector2(0.5f, 1);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 1);
            sub.rectTransform.pivot = new Vector2(0.5f, 1);
            sub.rectTransform.anchoredPosition = new Vector2(0, -66);
            sub.rectTransform.sizeDelta = new Vector2(600, 32);
            sub.alignment = TextAnchor.MiddleCenter;

            questList = new GameObject("QuestList").AddComponent<RectTransform>();
            questList.SetParent(panel.transform, false);
            questList.anchorMin = new Vector2(0.5f, 1);
            questList.anchorMax = new Vector2(0.5f, 1);
            questList.pivot = new Vector2(0.5f, 1);
            questList.anchoredPosition = new Vector2(0, -120);
            questList.sizeDelta = new Vector2(600, 3 * 110);

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => questPanel.SetActive(false));

            questPanel.SetActive(false);
        }

        void BuildQuestRows()
        {
            foreach (var r in questRows) Destroy(r);
            questRows.Clear();
            var qm = QuestManager.I;
            if (qm == null) return;
            float y = 0;
            for (int i = 0; i < qm.Today.Length; i++)
            {
                var q = qm.Today[i];
                if (q == null) continue;
                bool done = QuestManager.I.IsComplete(q);

                var row = CreateImage(questList, "QuestRow" + i, RoundedSprite(done ? Palette.Mint : Palette.White), new Color(1, 1, 1, 0.75f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(600, 96);
                row.type = Image.Type.Sliced;

                var name = CreateText(row.transform, "Name", q.Title, 26, Palette.Chocolate);
                name.rectTransform.anchorMin = new Vector2(0, 0);
                name.rectTransform.anchorMax = new Vector2(1, 1);
                name.rectTransform.offsetMin = new Vector2(20, 26);
                name.rectTransform.offsetMax = new Vector2(-170, 0);
                name.alignment = TextAnchor.MiddleLeft;

                var progress = CreateText(row.transform, "Progress", $"{q.Progress} / {q.Target}  ·  +{q.Reward} coins", 20, Palette.DeepPink);
                progress.rectTransform.anchorMin = new Vector2(0, 0);
                progress.rectTransform.anchorMax = new Vector2(1, 0);
                progress.rectTransform.offsetMin = new Vector2(20, 8);
                progress.rectTransform.offsetMax = new Vector2(-170, 34);
                progress.alignment = TextAnchor.MiddleLeft;

                var claim = CreateButton(row.transform, "Claim", done ? (q.Claimed ? "Done!" : "Claim") : "---", 24, Color.white, out var claimBg);
                Rt(claim).anchorMin = new Vector2(1, 0);
                Rt(claim).anchorMax = new Vector2(1, 1);
                Rt(claim).pivot = new Vector2(1, 0.5f);
                Rt(claim).anchoredPosition = new Vector2(-14, 0);
                Rt(claim).sizeDelta = new Vector2(130, 62);
                claimBg.color = done ? new Color(0.7f, 0.9f, 0.75f, 1) : new Color(0.85f, 0.8f, 0.9f, 0.7f);
                claimBg.sprite = RoundedSprite(done ? Palette.Mint : Palette.Lavender);
                claimBg.type = Image.Type.Sliced;
                claim.interactable = done && !q.Claimed;
                QuestDef captured = q;
                claim.onClick.AddListener(() => { QuestManager.I.Claim(captured); RefreshQuestPanel(); });

                questRows.Add(row.gameObject);
                y -= 110;
            }
        }

        public void OpenQuests()
        {
            RefreshQuestPanel();
            questPanel.SetActive(true);
        }

        public void RefreshQuestPanel()
        {
            if (questList == null || questPanel == null) return;
            if (!questPanel.activeSelf && QuestManager.I != null && QuestManager.I.RemainingRewards() > 0)
            {
                // keep panel live-updating while open; toast when something completes
            }
            BuildQuestRows();
        }

        void BuildTitle()
        {
            titlePanel = new GameObject("Title");
            titlePanel.transform.SetParent(Root, false);
titlePanel = new GameObject("Title");
            titlePanel.transform.SetParent(Root, false);
            var titleRt = titlePanel.AddComponent<RectTransform>();
            titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one;
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var backdrop = CreateImage(titlePanel.transform, "Backdrop", null, new Color(0.32f, 0.27f, 0.55f, 1f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var title = CreateText(titlePanel.transform, "Title", "Mochi Meadows", 110, Palette.White);
            AddOutline(title, 2f, new Color(0.30f, 0.24f, 0.36f, 0.8f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 0.62f);
            title.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            title.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            title.rectTransform.sizeDelta = new Vector2(1500, 130);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;

            var sub = CreateText(titlePanel.transform, "Sub", "a kawaii farming adventure", 34, new Color(1, 0.9f, 0.95f, 0.9f));
            sub.rectTransform.anchorMin = new Vector2(0.5f, 0.55f);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 0.55f);
            sub.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            sub.rectTransform.sizeDelta = new Vector2(1200, 60);
            sub.alignment = TextAnchor.MiddleCenter;

            var mochi = CreateImage(titlePanel.transform, "Mochi", SpriteBank.MochiIdle, Color.white);
            mochi.rectTransform.anchorMin = new Vector2(0.5f, 0.42f);
            mochi.rectTransform.anchorMax = new Vector2(0.5f, 0.42f);
            mochi.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mochi.rectTransform.sizeDelta = new Vector2(140, 140);

            var start = CreateButton(titlePanel.transform, "Start", "Start a cozy day", 34, Color.white, out var startBg);
            Rt(start).anchorMin = new Vector2(0.5f, 0.28f);
            Rt(start).anchorMax = new Vector2(0.5f, 0.28f);
            Rt(start).pivot = new Vector2(0.5f, 0.5f);
            Rt(start).sizeDelta = new Vector2(380, 78);
            startBg.color = new Color(0.75f, 0.95f, 0.8f, 1);
            startBg.sprite = RoundedSprite(Palette.Mint);
            startBg.type = Image.Type.Sliced;
            start.onClick.AddListener(() => { newGamePanel.SetActive(true); RefreshNewGame(); });

            var cont = CreateButton(titlePanel.transform, "Continue", "Continue", 28, Color.white, out var contBg);
            Rt(cont).anchorMin = new Vector2(0.5f, 0.19f);
            Rt(cont).anchorMax = new Vector2(0.5f, 0.19f);
            Rt(cont).pivot = new Vector2(0.5f, 0.5f);
            Rt(cont).sizeDelta = new Vector2(380, 64);
            contBg.color = new Color(0.9f, 0.8f, 0.65f, 1);
            contBg.sprite = RoundedSprite(Palette.Peach);
            contBg.type = Image.Type.Sliced;
            cont.onClick.AddListener(() => StartGame(true));
            cont.gameObject.SetActive(SaveSystem.HasSave);

            var hint = CreateText(titlePanel.transform, "Hint", "WASD / arrows to move   •   Space to act   •   keys 1-9 for tools   •   touch joystick + Act button on mobile", 22, new Color(1, 1, 1, 0.7f));
            hint.rectTransform.anchorMin = new Vector2(0.5f, 0.08f);
            hint.rectTransform.anchorMax = new Vector2(0.5f, 0.08f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            hint.rectTransform.sizeDelta = new Vector2(1800, 40);
            hint.alignment = TextAnchor.MiddleCenter;

            // settings gear on title
            var controlsBtn = CreateButton(titlePanel.transform, "ControlsBtn", "Controls", 24, Color.white, out var controlsBtnBg);
            Rt(controlsBtn).anchorMin = new Vector2(0.5f, 0.11f);
            Rt(controlsBtn).anchorMax = new Vector2(0.5f, 0.11f);
            Rt(controlsBtn).pivot = new Vector2(0.5f, 0.5f);
            Rt(controlsBtn).sizeDelta = new Vector2(220, 56);
            controlsBtnBg.color = new Color(0.9f, 0.84f, 0.98f, 1);
            controlsBtnBg.sprite = RoundedSprite(Palette.Lavender);
            controlsBtnBg.type = Image.Type.Sliced;
            controlsBtn.onClick.AddListener(() => controlsPanel.SetActive(true));

            var gear = CreateButton(titlePanel.transform, "Gear", "⚙", 30, Color.white, out var gearBg);
            Rt(gear).anchorMin = new Vector2(1, 1);
            Rt(gear).anchorMax = new Vector2(1, 1);
            Rt(gear).pivot = new Vector2(1, 1);
            Rt(gear).anchoredPosition = new Vector2(-18, -18);
            Rt(gear).sizeDelta = new Vector2(56, 56);
            gearBg.color = new Color(0.85f, 0.8f, 0.95f, 1);
            gearBg.sprite = RoundedSprite(Palette.Lavender);
            gearBg.type = Image.Type.Sliced;
            gear.onClick.AddListener(() => OpenMenu());

            var credit = CreateText(titlePanel.transform, "Credit", "made with kawaii · Mochi Meadows", 18, new Color(1, 1, 1, 0.5f));
            credit.rectTransform.anchorMin = new Vector2(0.5f, 0);
            credit.rectTransform.anchorMax = new Vector2(0.5f, 0);
            credit.rectTransform.pivot = new Vector2(0.5f, 0);
            credit.rectTransform.anchoredPosition = new Vector2(0, 12);
            credit.rectTransform.sizeDelta = new Vector2(1200, 24);
            credit.alignment = TextAnchor.MiddleCenter;

            StartGameRoutine();
        }

        void StartGameRoutine()
        {
            // keep title visible until GameBootstrap says go
        }

        public void StartGame(bool load)
        {
            titlePanel.SetActive(false);
            if (AudioService.I != null) AudioService.I.SetTitleMode(false);
            GameManager.I.StartGame(load, this);
        }

        void BuildNewGame()
        {
            newGamePanel = new GameObject("NewGamePanel");
            newGamePanel.transform.SetParent(Root, false);
            var nRt = newGamePanel.AddComponent<RectTransform>();
            nRt.anchorMin = Vector2.zero; nRt.anchorMax = Vector2.one;
            nRt.offsetMin = Vector2.zero; nRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(newGamePanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(newGamePanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(560, 400);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "New Game", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(440, 46);
            title.alignment = TextAnchor.MiddleCenter;

            // day length
            var dayLabel = CreateText(panel.transform, "DayLabel", "Day length", 24, Palette.Chocolate);
            dayLabel.rectTransform.anchorMin = new Vector2(0, 1);
            dayLabel.rectTransform.anchorMax = new Vector2(0, 1);
            dayLabel.rectTransform.pivot = new Vector2(0, 1);
            dayLabel.rectTransform.anchoredPosition = new Vector2(36, -84);
            dayLabel.rectTransform.sizeDelta = new Vector2(150, 40);
            dayLabel.alignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var btn = CreateButton(panel.transform, "Day" + i, GameManager.DayLengthNames[i], 20, Color.white, out var bg);
                Rt(btn).anchorMin = new Vector2(0.5f, 1);
                Rt(btn).anchorMax = new Vector2(0.5f, 1);
                Rt(btn).pivot = new Vector2(0.5f, 1);
                Rt(btn).anchoredPosition = new Vector2(-95 + i * 110, -84);
                Rt(btn).sizeDelta = new Vector2(100, 52);
                bg.sprite = RoundedSprite(Palette.BabyBlue);
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.8f, 0.87f, 0.98f, 1);
                btn.onClick.AddListener(() => { GameManager.I.SetDayLength(idx); RefreshNewGame(); });
            }

            var fundsLabel = CreateText(panel.transform, "FundsLabel", "Starting coins", 24, Palette.Chocolate);
            fundsLabel.rectTransform.anchorMin = new Vector2(0, 1);
            fundsLabel.rectTransform.anchorMax = new Vector2(0, 1);
            fundsLabel.rectTransform.pivot = new Vector2(0, 1);
            fundsLabel.rectTransform.anchoredPosition = new Vector2(36, -150);
            fundsLabel.rectTransform.sizeDelta = new Vector2(180, 40);
            fundsLabel.alignment = TextAnchor.MiddleLeft;

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var btn = CreateButton(panel.transform, "Funds" + i, GameManager.FundsNames[i], 18, Color.white, out var bg);
                Rt(btn).anchorMin = new Vector2(0.5f, 1);
                Rt(btn).anchorMax = new Vector2(0.5f, 1);
                Rt(btn).pivot = new Vector2(0.5f, 1);
                Rt(btn).anchoredPosition = new Vector2(-95 + i * 110, -150);
                Rt(btn).sizeDelta = new Vector2(100, 52);
                bg.sprite = RoundedSprite(Palette.Peach);
                bg.type = Image.Type.Sliced;
                bg.color = new Color(0.98f, 0.88f, 0.75f, 1);
                btn.onClick.AddListener(() => { GameManager.I.SetFunds(idx); RefreshNewGame(); });
            }

            var start = CreateButton(panel.transform, "Start", "Start a cozy day", 28, Color.white, out var startBg);
            Rt(start).anchorMin = new Vector2(0.5f, 0);
            Rt(start).anchorMax = new Vector2(0.5f, 0);
            Rt(start).pivot = new Vector2(0.5f, 0);
            Rt(start).anchoredPosition = new Vector2(0, 26);
            Rt(start).sizeDelta = new Vector2(300, 62);
            startBg.color = new Color(0.75f, 0.95f, 0.8f, 1);
            startBg.sprite = RoundedSprite(Palette.Mint);
            startBg.type = Image.Type.Sliced;
            start.onClick.AddListener(() => { newGamePanel.SetActive(false); StartGame(false); });

            var back = CreateButton(panel.transform, "Back", "← Back", 20, Color.white, out var backBg);
            Rt(back).anchorMin = new Vector2(0, 0);
            Rt(back).anchorMax = new Vector2(0, 0);
            Rt(back).pivot = new Vector2(0, 0);
            Rt(back).anchoredPosition = new Vector2(20, 18);
            Rt(back).sizeDelta = new Vector2(110, 44);
            backBg.color = new Color(0.9f, 0.84f, 0.98f, 1);
            backBg.sprite = RoundedSprite(Palette.Lavender);
            backBg.type = Image.Type.Sliced;
            back.onClick.AddListener(() => newGamePanel.SetActive(false));

            var closeBtn = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(closeBtn).anchorMin = new Vector2(1, 1);
            Rt(closeBtn).anchorMax = new Vector2(1, 1);
            Rt(closeBtn).pivot = new Vector2(1, 1);
            Rt(closeBtn).anchoredPosition = new Vector2(-14, -14);
            Rt(closeBtn).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            closeBtn.onClick.AddListener(() => newGamePanel.SetActive(false));

            newGamePanel.SetActive(false);
        }

        void RefreshNewGame()
        {
            // highlight the selected options
            if (newGamePanel == null) return;
            var gm = GameManager.I;
            for (int i = 0; i < 3; i++)
            {
                var dayBtn = newGamePanel.transform.Find("Panel/Day" + i);
                var fundsBtn = newGamePanel.transform.Find("Panel/Funds" + i);
                if (dayBtn != null)
                    dayBtn.GetComponent<Image>().color = i == gm.DayLengthOption ? Color.white : new Color(0.8f, 0.87f, 0.98f, 0.5f);
                if (fundsBtn != null)
                    fundsBtn.GetComponent<Image>().color = i == gm.StartMoneyOption ? Color.white : new Color(0.98f, 0.88f, 0.75f, 0.5f);
            }
        }

        void BuildMenu()
        {
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(Root, false);
            var menuRt = menuPanel.AddComponent<RectTransform>();
            menuRt.anchorMin = Vector2.zero; menuRt.anchorMax = Vector2.one;
            menuRt.offsetMin = Vector2.zero; menuRt.offsetMax = Vector2.zero;
            var backdrop = CreateImage(menuPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(menuPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(520, 620);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Pause", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(440, 48);
            title.alignment = TextAnchor.MiddleCenter;

            // --- actions (left column) ---
            float y = -92;
            var resume = CreateButton(panel.transform, "Resume", "▶ Resume", 26, Color.white, out var resumeBg);
            menuResumeBtn = resume;
            Rt(resume).anchorMin = new Vector2(0.5f, 1);
            Rt(resume).anchorMax = new Vector2(0.5f, 1);
            Rt(resume).pivot = new Vector2(0.5f, 1);
            Rt(resume).anchoredPosition = new Vector2(0, y);
            Rt(resume).sizeDelta = new Vector2(440, 62);
            resumeBg.color = new Color(0.75f, 0.95f, 0.8f, 1);
            resumeBg.sprite = RoundedSprite(Palette.Mint);
            resumeBg.type = Image.Type.Sliced;
            resume.onClick.AddListener(() => CloseMenu());
            y -= 76;

            var save = CreateButton(panel.transform, "Save", "Save now", 26, Color.white, out var saveBg);
            menuSaveBtn = save;
            Rt(save).anchorMin = new Vector2(0.5f, 1);
            Rt(save).anchorMax = new Vector2(0.5f, 1);
            Rt(save).pivot = new Vector2(0.5f, 1);
            Rt(save).anchoredPosition = new Vector2(0, y);
            Rt(save).sizeDelta = new Vector2(440, 62);
            saveBg.color = new Color(0.8f, 0.87f, 0.98f, 1);
            saveBg.sprite = RoundedSprite(Palette.BabyBlue);
            saveBg.type = Image.Type.Sliced;
            save.onClick.AddListener(() => { SaveSystem.Save(GameManager.I); GameManager.I.Announce("Saved! <3"); });
            y -= 76;

            var controls = CreateButton(panel.transform, "Controls", "Controls", 26, Color.white, out var controlsBg);
            Rt(controls).anchorMin = new Vector2(0.5f, 1);
            Rt(controls).anchorMax = new Vector2(0.5f, 1);
            Rt(controls).pivot = new Vector2(0.5f, 1);
            Rt(controls).anchoredPosition = new Vector2(0, y);
            Rt(controls).sizeDelta = new Vector2(440, 62);
            controlsBg.color = new Color(0.9f, 0.84f, 0.98f, 1);
            controlsBg.sprite = RoundedSprite(Palette.Lavender);
            controlsBg.type = Image.Type.Sliced;
            controls.onClick.AddListener(() => controlsPanel.SetActive(true));
            y -= 76;

            var toTitle = CreateButton(panel.transform, "ToTitle", "Return to Title", 26, Color.white, out var toTitleBg);
            menuTitleBtn = toTitle;
            Rt(toTitle).anchorMin = new Vector2(0.5f, 1);
            Rt(toTitle).anchorMax = new Vector2(0.5f, 1);
            Rt(toTitle).pivot = new Vector2(0.5f, 1);
            Rt(toTitle).anchoredPosition = new Vector2(0, y);
            Rt(toTitle).sizeDelta = new Vector2(440, 62);
            toTitleBg.color = new Color(0.98f, 0.88f, 0.75f, 1);
            toTitleBg.sprite = RoundedSprite(Palette.Peach);
            toTitleBg.type = Image.Type.Sliced;
            toTitle.onClick.AddListener(() => ReturnToTitle());
            y -= 76;

            var quit = CreateButton(panel.transform, "Quit", "Save & quit", 24, Color.white, out var quitBg);
            menuQuitBtn = quit;
            Rt(quit).anchorMin = new Vector2(0.5f, 1);
            Rt(quit).anchorMax = new Vector2(0.5f, 1);
            Rt(quit).pivot = new Vector2(0.5f, 1);
            Rt(quit).anchoredPosition = new Vector2(0, y);
            Rt(quit).sizeDelta = new Vector2(440, 58);
            quitBg.color = new Color(0.95f, 0.8f, 0.8f, 1);
            quitBg.sprite = RoundedSprite(Palette.Blush);
            quitBg.type = Image.Type.Sliced;
            quit.onClick.AddListener(() =>
            {
                SaveSystem.Save(GameManager.I);
                CloseMenu();
#if UNITY_STANDALONE || UNITY_EDITOR
                Application.Quit();
#endif
            });
            y -= 70;

            // --- settings (sound) ---
            var settingsTitle = CreateText(panel.transform, "SettingsTitle", "~ Sound ~", 22, Palette.DeepPink);
            settingsTitle.rectTransform.anchorMin = new Vector2(0.5f, 1);
            settingsTitle.rectTransform.anchorMax = new Vector2(0.5f, 1);
            settingsTitle.rectTransform.pivot = new Vector2(0.5f, 1);
            settingsTitle.rectTransform.anchoredPosition = new Vector2(0, y);
            settingsTitle.rectTransform.sizeDelta = new Vector2(440, 30);
            settingsTitle.alignment = TextAnchor.MiddleCenter;
            y -= 40;

            var musicLabel = CreateText(panel.transform, "MusicLabel", "Music", 24, Palette.Chocolate);
            musicLabel.rectTransform.anchorMin = new Vector2(0.5f, 1);
            musicLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
            musicLabel.rectTransform.pivot = new Vector2(0.5f, 1);
            musicLabel.rectTransform.anchoredPosition = new Vector2(-120, y);
            musicLabel.rectTransform.sizeDelta = new Vector2(100, 30);
            musicLabel.alignment = TextAnchor.MiddleRight;

            var musicSlider = AddSlider(panel.transform, "MusicSlider", new Vector2(60, y), 220);
            musicSlider.value = 1f;
            musicSlider.onValueChanged.AddListener(v => { if (AudioService.I != null) AudioService.I.MusicVolume = v; });
            y -= 52;

            var sfxLabel = CreateText(panel.transform, "SfxLabel", "SFX", 24, Palette.Chocolate);
            sfxLabel.rectTransform.anchorMin = new Vector2(0.5f, 1);
            sfxLabel.rectTransform.anchorMax = new Vector2(0.5f, 1);
            sfxLabel.rectTransform.pivot = new Vector2(0.5f, 1);
            sfxLabel.rectTransform.anchoredPosition = new Vector2(-120, y);
            sfxLabel.rectTransform.sizeDelta = new Vector2(100, 30);
            sfxLabel.alignment = TextAnchor.MiddleRight;

            var sfxSlider = AddSlider(panel.transform, "SfxSlider", new Vector2(60, y), 220);
            sfxSlider.value = 1f;
            sfxSlider.onValueChanged.AddListener(v => { if (AudioService.I != null) AudioService.I.SfxVolume = v; });

            var close = CreateButton(panel.transform, "Close", "✕", 30, Color.white, out var closeBg);
            Rt(close).anchorMin = new Vector2(1, 1);
            Rt(close).anchorMax = new Vector2(1, 1);
            Rt(close).pivot = new Vector2(1, 1);
            Rt(close).anchoredPosition = new Vector2(-14, -14);
            Rt(close).sizeDelta = new Vector2(52, 52);
            closeBg.color = new Color(0.9f, 0.6f, 0.65f, 1);
            closeBg.sprite = RoundedSprite(Palette.Blush);
            closeBg.type = Image.Type.Sliced;
            close.onClick.AddListener(() => CloseMenu());

            menuPanel.SetActive(false);
        }

        // Runtime-built uGUI slider (background + fill + handle).
        Slider AddSlider(Transform parent, string name, Vector2 anchoredPos, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1);
            rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(width, 24);

            var bg = CreateImage(go.transform, "Background", RoundedSprite(Palette.Lavender), new Color(0.8f, 0.75f, 0.9f, 0.9f));
            bg.type = Image.Type.Sliced;
            bg.rectTransform.anchorMin = Vector2.zero;
            bg.rectTransform.anchorMax = Vector2.one;
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            var fill = CreateImage(go.transform, "Fill Area", null, Color.white);
            fill.rectTransform.anchorMin = new Vector2(0, 0);
            fill.rectTransform.anchorMax = new Vector2(1, 1);
            fill.rectTransform.offsetMin = new Vector2(5, 5);
            fill.rectTransform.offsetMax = new Vector2(-5, -5);
            var fillImg = CreateImage(fill.transform, "Fill", RoundedSprite(Palette.DeepPink), Color.white);
            fillImg.type = Image.Type.Sliced;
            fillImg.rectTransform.anchorMin = new Vector2(0, 0);
            fillImg.rectTransform.anchorMax = new Vector2(1, 1);
            fillImg.rectTransform.offsetMin = Vector2.zero;
            fillImg.rectTransform.offsetMax = Vector2.zero;

            var handle = CreateImage(go.transform, "Handle Slide Area", null, Color.white);
            handle.rectTransform.anchorMin = new Vector2(0, 0);
            handle.rectTransform.anchorMax = new Vector2(1, 1);
            handle.rectTransform.offsetMin = new Vector2(5, 5);
            handle.rectTransform.offsetMax = new Vector2(-5, -5);
            var handleImg = CreateImage(handle.transform, "Handle", RoundedSprite(Palette.Cream), Color.white);
            handleImg.type = Image.Type.Sliced;
            handleImg.rectTransform.anchorMin = new Vector2(0, 0);
            handleImg.rectTransform.anchorMax = new Vector2(1, 1);
            handleImg.rectTransform.offsetMin = Vector2.zero;
            handleImg.rectTransform.offsetMax = Vector2.zero;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillImg.rectTransform;
            slider.handleRect = handleImg.rectTransform;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        void BuildControls()
        {
            controlsPanel = new GameObject("ControlsPanel");
            controlsPanel.transform.SetParent(Root, false);
            var cRt = controlsPanel.AddComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero; cRt.anchorMax = Vector2.one;
            cRt.offsetMin = Vector2.zero; cRt.offsetMax = Vector2.zero;

            var backdrop = CreateImage(controlsPanel.transform, "Backdrop", null, new Color(0.15f, 0.12f, 0.3f, 0.55f));
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.rectTransform.SetAsFirstSibling();

            var panel = CreateImage(controlsPanel.transform, "Panel", RoundedSprite(Palette.Cream), Color.white);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(680, 700);
            panel.type = Image.Type.Sliced;

            var title = CreateText(panel.transform, "Title", "Controls", 36, Palette.Chocolate);
            AddOutline(title, 1f, new Color(0.36f, 0.28f, 0.40f, 0.5f));
            title.rectTransform.anchorMin = new Vector2(0.5f, 1);
            title.rectTransform.anchorMax = new Vector2(0.5f, 1);
            title.rectTransform.pivot = new Vector2(0.5f, 1);
            title.rectTransform.anchoredPosition = new Vector2(0, -22);
            title.rectTransform.sizeDelta = new Vector2(600, 48);
            title.alignment = TextAnchor.MiddleCenter;

            var list = new GameObject("ControlsList").AddComponent<RectTransform>();
            list.SetParent(panel.transform, false);
            list.anchorMin = new Vector2(0.5f, 1);
            list.anchorMax = new Vector2(0.5f, 1);
            list.pivot = new Vector2(0.5f, 1);
            list.anchoredPosition = new Vector2(0, -84);
            list.sizeDelta = new Vector2(620, 540);

            float y = 0;
            AddControlSection(list, "Desktop", new[]
            {
                ("WASD / arrow keys", "Move"),
                ("Space / E / click", "Act: use tool, talk, harvest"),
                ("1 - 9 / scroll wheel", "Choose hotbar item"),
                ("Esc", "Pause menu"),
            }, ref y);
            AddControlSection(list, "Touch", new[]
            {
                ("Drag left side", "Move (joystick)"),
                ("Tap a tile", "Act with the selected tool"),
                ("Act button", "Act on the tile you face"),
                ("Tap Mochi / pond / kitchen", "Shop / fishing / cooking"),
            }, ref y);
            AddControlSection(list, "Gamepad", new[]
            {
                ("Left stick", "Move"),
                ("A button", "Act"),
                ("Start / M", "Pause menu"),
                ("B button", "Back / close UI"),
            }, ref y);

            var back = CreateButton(panel.transform, "Back", "← Back", 26, Color.white, out var backBg);
            Rt(back).anchorMin = new Vector2(0.5f, 0);
            Rt(back).anchorMax = new Vector2(0.5f, 0);
            Rt(back).pivot = new Vector2(0.5f, 0);
            Rt(back).anchoredPosition = new Vector2(0, 22);
            Rt(back).sizeDelta = new Vector2(200, 56);
            backBg.color = new Color(0.9f, 0.84f, 0.98f, 1);
            backBg.sprite = RoundedSprite(Palette.Lavender);
            backBg.type = Image.Type.Sliced;
            back.onClick.AddListener(() => controlsPanel.SetActive(false));

            controlsPanel.SetActive(false);
        }

        void AddControlSection(RectTransform list, string header, (string keys, string action)[] rows, ref float y)
        {
            var h = CreateText(list, "H" + header, "~ " + header + " ~", 22, Palette.DeepPink);
            h.rectTransform.anchorMin = new Vector2(0.5f, 1);
            h.rectTransform.anchorMax = new Vector2(0.5f, 1);
            h.rectTransform.pivot = new Vector2(0.5f, 1);
            h.rectTransform.anchoredPosition = new Vector2(0, y);
            h.rectTransform.sizeDelta = new Vector2(560, 28);
            h.alignment = TextAnchor.MiddleCenter;
            y -= 32;
            foreach (var (keys, action) in rows)
            {
                var row = CreateImage(list, "Row" + keys, RoundedSprite(Palette.White), new Color(1, 1, 1, 0.7f));
                row.rectTransform.anchorMin = new Vector2(0.5f, 1);
                row.rectTransform.anchorMax = new Vector2(0.5f, 1);
                row.rectTransform.pivot = new Vector2(0.5f, 1);
                row.rectTransform.anchoredPosition = new Vector2(0, y);
                row.rectTransform.sizeDelta = new Vector2(560, 52);
                row.type = Image.Type.Sliced;

                var k = CreateText(row.transform, "Keys", keys, 22, Palette.Chocolate);
                k.rectTransform.anchorMin = new Vector2(0, 0);
                k.rectTransform.anchorMax = new Vector2(1, 1);
                k.rectTransform.offsetMin = new Vector2(20, 0);
                k.rectTransform.offsetMax = new Vector2(-280, 0);
                k.alignment = TextAnchor.MiddleLeft;

                var a = CreateText(row.transform, "Action", action, 19, Palette.DeepPink);
                a.rectTransform.anchorMin = new Vector2(0, 0);
                a.rectTransform.anchorMax = new Vector2(1, 1);
                a.rectTransform.offsetMin = new Vector2(290, 0);
                a.rectTransform.offsetMax = new Vector2(-20, 0);
                a.alignment = TextAnchor.MiddleLeft;
                y -= 58;
            }
            y -= 10;
        }

        public void OpenMenu()
        {
            bool started = GameManager.I != null && GameManager.I.GameStarted;
            if (menuSaveBtn != null) menuSaveBtn.gameObject.SetActive(started);
            if (menuQuitBtn != null) menuQuitBtn.gameObject.SetActive(started);
            if (menuResumeBtn != null) menuResumeBtn.gameObject.SetActive(started);
            if (menuTitleBtn != null) menuTitleBtn.gameObject.SetActive(started);
            menuPanel.SetActive(true);
            if (GameManager.I != null && started) GameManager.I.IsPaused = true;
        }

        public void CloseMenu()
        {
            menuPanel.SetActive(false);
            if (GameManager.I != null) GameManager.I.IsPaused = false;
        }

        public void ReturnToTitle()
        {
            if (AudioService.I != null) AudioService.I.SetTitleMode(true);
            var gm = GameManager.I;
            if (gm != null && gm.GameStarted)
            {
                SaveSystem.Save(gm);
                gm.GameStarted = false;
                gm.IsPaused = false;
            }
            if (shopPanel != null) shopPanel.SetActive(false);
            if (questPanel != null) questPanel.SetActive(false);
            if (makeoverPanel != null) makeoverPanel.SetActive(false);
            if (decorPanel != null) decorPanel.SetActive(false);
            if (fishPanel != null) fishPanel.SetActive(false);
            if (cookPanel != null) cookPanel.SetActive(false);
            if (scrapPanel != null) scrapPanel.SetActive(false);
            if (controlsPanel != null) controlsPanel.SetActive(false);
            CloseMenu();
            titlePanel.SetActive(true);
            RefreshTitleButtons();
        }

        public void ShowNewGame()
        {
            RefreshNewGame();
            newGamePanel.SetActive(true);
        }

        public void ShowControls()
        {
            if (controlsPanel != null) controlsPanel.SetActive(true);
        }

        public void RefreshTitleButtons()
        {
            var cont = titlePanel != null ? titlePanel.transform.Find("Continue") : null;
            if (cont != null) cont.gameObject.SetActive(SaveSystem.HasSave);
        }

        // --- refresh helpers ---
        public void RefreshAll()
        {
            RefreshClock();
            RefreshCoins();
            RefreshEnergy();
            RefreshHotbar();
        }

        public void RefreshClock()
        {
            var gm = GameManager.I;
            if (gm == null || dayText == null) return;
            dayText.text = $"Day {gm.Day} · {gm.SeasonName}";
            clockText.text = gm.ClockText;
        }
        public void RefreshCoins()
        {
            var gm = GameManager.I;
            if (gm == null || coinText == null) return;
            coinText.text = gm.Money.ToString();
            if (shopCoins != null) shopCoins.text = $"{gm.Money} coins";
        }

        void RefreshEnergy()
        {
            var gm = GameManager.I;
            if (gm == null) return;
            int full = Mathf.CeilToInt(gm.Energy / 10f);
            for (int i = 0; i < 10; i++)
            {
                if (energyHearts[i] == null) continue;
                energyHearts[i].color = i < full ? Palette.DeepPink : new Color(0.25f, 0.2f, 0.35f, 0.25f);
                energyHearts[i].transform.localScale = i < full ? Vector3.one * (1f + 0.03f * Mathf.Sin(Time.time * 3f + i)) : Vector3.one;
            }
        }

        public void RefreshHotbar()
        {
            var gm = GameManager.I;
            if (gm == null || hotbarSlots[0] == null) return;
            Sprite[] icons = { SpriteBank.HoeIcon, SpriteBank.CanIcon, SpriteBank.CropSprites[0, 3], SpriteBank.CropSprites[1, 3], SpriteBank.CropSprites[2, 3], SpriteBank.CropSprites[3, 3], SpriteBank.CropSprites[4, 3], SpriteBank.CropSprites[5, 3], SpriteBank.HandIcon };
            for (int i = 0; i < 9; i++)
            {
                hotbarIcons[i].sprite = icons[i];
                hotbarSlots[i].color = i == gm.SelectedSlot ? new Color(1f, 0.85f, 0.9f, 1f) : Color.white;
                int item = gm.Hotbar[i];
                int count = -1;
                if (item >= 2 && item <= 7) count = gm.Seeds[item - 2];
                hotbarCounts[i].text = count >= 0 ? count.ToString() : "";
                hotbarCounts[i].gameObject.SetActive(count > 0);
            }
        }

        // --- toast ---
        public void ShowToast(string msg)
        {
            if (toastText == null) return;
            StopCoroutine("ToastRoutine");
            toastText.gameObject.SetActive(true);
            toastText.transform.parent.gameObject.SetActive(true);
            toastText.text = msg;
            StartCoroutine("ToastRoutine");
        }

        IEnumerator ToastRoutine()
        {
            yield return new WaitForSeconds(2.8f);
            if (toastText != null)
            {
                toastText.gameObject.SetActive(false);
                toastText.transform.parent.gameObject.SetActive(false);
            }
        }

        // --- sleep fade ---
        public IEnumerator FadeToBlack(bool fadeIn, float duration)
        {
            if (fadeImage == null) yield break;
            float t = 0;
            Color c = fadeImage.color;
            float from = fadeIn ? 0 : 1;
            float to = fadeIn ? 1 : 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(from, to, Mathf.SmoothStep(0, 1, t / duration));
                fadeImage.color = c;
                yield return null;
            }
            c.a = to;
            fadeImage.color = c;
        }

        // --- builders ---
        public static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            return img;
        }

        public static Text CreateText(Transform parent, string name, string text, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = FontHelper.Get();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button CreateButton(Transform parent, string name, string label, int size, Color textColor, out Image bg)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            // pastel buttons need dark text: white-on-light is illegible.
            // Callers that truly want white (e.g. color swatches) set the label
            // color explicitly after creation.
            Color text = (textColor.r > 0.85f && textColor.g > 0.85f && textColor.b > 0.85f)
                ? Palette.Chocolate
                : textColor;
            var labelGo = CreateText(go.transform, "Label", label, size, text);
            var rt = labelGo.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            labelGo.alignment = TextAnchor.MiddleCenter;
            bg = img;
            return btn;
        }

        public static Button CreateButton(Transform parent, string name, string label, int size, Color textColor)
        {
            return CreateButton(parent, name, label, size, textColor, out _);
        }


        static RectTransform Rt(Component c) => (RectTransform)c.transform;

        static void AddOutline(Text t, float width, Color color)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(width, -width);
        }

        // Rounded pastel panel sprite (procedural).
        static Sprite _rounded;
        public static Sprite RoundedSprite(Color color)
        {
            // reuse a shared white rounded sprite; tint via Image.color
            if (_rounded == null)
            {
                var ps = new PixelSprite { Name = "rounded", Width = 16, Height = 16 };
                var cols = new List<char> { 'a' };
                // build rounded square by scanning rows of ASCII
                string[] art =
                {
                    "................", "................",
                    "..aaaaaaaaaaaa..", ".aaaaaaaaaaaaaa.",
                    ".aaaaaaaaaaaaaa.", ".aaaaaaaaaaaaaa.",
                    ".aaaaaaaaaaaaaa.", ".aaaaaaaaaaaaaa.",
                    ".aaaaaaaaaaaaaa.", ".aaaaaaaaaaaaaa.",
                    ".aaaaaaaaaaaaaa.", ".aaaaaaaaaaaaaa.",
                    ".aaaaaaaaaaaaaa.", "..aaaaaaaaaaaa..",
                    "................", "................",
                };
                _rounded = SpriteFactory.ToSprite(PixelSprite.From("rounded", art,
                    new Dictionary<char, Color> { ['a'] = Color.white }));
            }
            return _rounded;
        }
    }

    public static class FontHelper
    {
        static Font _font;
        static bool tried;
        public static Font Get()
        {
            if (_font != null) return _font;
            if (tried) return _font ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tried = true;
            try
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { _font = null; }
            }
            return _font;
        }
    }
}
