using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This class exposes the the game model in the inspector, and ticks the
    /// simulation.
    /// </summary> 
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        //This model field is public and can be therefore be modified in the 
        //inspector.
        //The reference actually comes from the InstanceRegister, and is shared
        //through the simulation and events. Unity will deserialize over this
        //shared reference when the scene loads, allowing the model to be
        //conveniently configured inside the inspector.
        public PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public float CatcherSince;

        [UsedImplicitly]
        private void OnEnable()
        {
            Instance = this;

            Thread.Sleep(2000); // a hack to get game objects initialized
            SetupLevel(1);
            SetupPlayers(null);
        }

        [UsedImplicitly]
        private void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        [UsedImplicitly]
        private void Update()
        {
            if (Instance == this) Simulation.Tick();
        }

        public void SetupLevel(int level)
        {
            var levelContainer = GameObject.Find("Levels");
            foreach (Transform child in levelContainer.transform)
            {
                if (child.name.StartsWith("L") == false) continue;
                if (child.name == $"L{level}")
                {
                    if (child.gameObject.activeSelf == false) child.gameObject.SetActive(true);
                }
                else
                {
                    if (child.gameObject.activeSelf) child.gameObject.SetActive(false);
                }
            }
        }

        public void SetupPlayers(int? activePlayerCount)
        {
            if (activePlayerCount != null) model.activePlayerCount = activePlayerCount.Value;

            var catcherIndex = Random.Range(0, model.activePlayerCount);
            var activePlayers = new List<PlayerController>();
            for (var i = 0; i < model.players.Length; i++)
            {
                var player = model.players[i];
                if (i < model.activePlayerCount)
                {
                    if (player.gameObject.activeSelf == false)
                        player.gameObject.SetActive(true);

                    if (i == catcherIndex)
                        player.MakeCatcher(null);
                    else
                        player.UnmakeCatcher(true);

                    activePlayers.Add(player);
                }
                else
                {
                    player.gameObject.SetActive(false);
                }
            }
            model.activePlayers = activePlayers.ToArray();
        }

        public float GetCatcherTime() => Time.time - CatcherSince;
    }
}