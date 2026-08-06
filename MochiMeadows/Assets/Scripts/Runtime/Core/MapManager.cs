using UnityEngine;
using MochiMeadows.Art;
using MochiMeadows.Audio;

namespace MochiMeadows.Core
{
    // Two maps: the meadow (outdoor) and the cozy farmhouse interior.
    // Entering/leaving toggles the world roots, teleports the player, and
    // hides outdoor-only systems (weather, sky, Mochi, chickens).
    public class MapManager : MonoBehaviour
    {
        public static MapManager I;

        public bool Indoors { get; private set; }

        Transform meadowRoot, houseRoot;
        Camera mainCam;
        Game.GameManager gm;

        public RectInt CurrentBounds
        {
            get
            {
                var lvl = LevelConfig.Current;
                return Indoors
                    ? new RectInt(0, 0, lvl.interior.width, lvl.interior.height)
                    : new RectInt(0, 0, lvl.world.width, lvl.world.height);
            }
        }

        public Vector3 BedPos { get; private set; }
        public Vector3 KitchenPos { get; private set; }

        public void Init(Transform meadow, Transform house, Camera cam, Game.GameManager gameManager)
        {
            I = this;
            meadowRoot = meadow;
            houseRoot = house;
            mainCam = cam;
            gm = gameManager;

            var lvl = LevelConfig.Current;
            BedPos = new Vector3(lvl.interior.bed.x, lvl.interior.bed.y, 0);
            KitchenPos = new Vector3(lvl.interior.kitchen.x, lvl.interior.kitchen.y, 0);

            if (houseRoot != null) houseRoot.gameObject.SetActive(false);
        }

        public void EnterHouse(Game.GameManager gm, Game.PlayerController player)
        {
            if (Indoors) return;
            Indoors = true;
            mainCam.backgroundColor = new Color(0.38f, 0.32f, 0.52f, 1);
            if (houseRoot != null) houseRoot.gameObject.SetActive(true);
            if (meadowRoot != null) meadowRoot.gameObject.SetActive(false);
            var lvl = LevelConfig.Current;
            var door = lvl.spots.interiorDoor;
            player.TeleportTo(new Vector3(door[0] + 0.5f, door[1] + 0.5f, -1));
            HideOutdoorSystems(false);
            gm.Announce("Welcome home! The fireplace is warm~");
            gm.Audio.Play(AudioService.Sfx.UISelect);
        }

        public void ExitHouse(Game.GameManager gm, Game.PlayerController player)
        {
            if (!Indoors) return;
            Indoors = false;
            mainCam.backgroundColor = new Color(0.72f, 0.82f, 0.95f, 1);
            if (houseRoot != null) houseRoot.gameObject.SetActive(false);
            if (meadowRoot != null) meadowRoot.gameObject.SetActive(true);
            var lvl = LevelConfig.Current;
            var door = lvl.spots.houseDoor;
            player.TeleportTo(new Vector3(door[0] + 0.5f, door[1] + 0.5f, -1));
            HideOutdoorSystems(true);
            gm.Announce("Back to the fresh air~");
        }

        void HideOutdoorSystems(bool show)
        {
            if (gm == null) return;
            if (gm.Mochi != null) gm.Mochi.gameObject.SetActive(show);
            if (gm.Critters != null)
                for (int i = 0; i < gm.Critters.Length; i++)
                    if (gm.Critters[i] != null) gm.Critters[i].gameObject.SetActive(show);
            // Weather/day-night live on the bootstrap object; gate via flag instead
            var weather = FindObjectOfType<Game.WeatherController>();
            if (weather != null) weather.Active = show;
            var dayNight = FindObjectOfType<Game.DayNightController>();
            if (dayNight != null) dayNight.Active = show;
        }
    }
}
