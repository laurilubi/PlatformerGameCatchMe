using System.Collections.Generic;
using System.Linq;
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

        public void OnEnable()
        {
            Instance = this;

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

        void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Instance == this) Simulation.Tick();
        }
    }
}