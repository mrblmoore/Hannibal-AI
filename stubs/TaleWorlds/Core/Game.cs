using System;

namespace TaleWorlds.Core
{
    /// <summary>
    /// Stub implementation of the Game class for Bannerlord
    /// </summary>
    public class Game
    {
        private static Game _current;

        public static Game Current
        {
            get => _current ?? (_current = new Game());
            set => _current = value;
        }

        public GameStateManager StateManager { get; private set; }

        public Game()
        {
            StateManager = new GameStateManager();
        }
    }

    /// <summary>
    /// Manages different game states (menu, campaign, battle, etc.)
    /// </summary>
    public class GameStateManager
    {
        public void RegisterState<T>(T state) where T : GameState
        {
            // Stub implementation
        }

        public void CleanStates()
        {
            // Stub implementation
        }
        
        public void PushState(GameState state)
        {
            // Stub implementation
        }
    }

    /// <summary>
    /// Base class for all game states (menu, campaign, battle, etc.)
    /// </summary>
    public abstract class GameState
    {
        public virtual void OnActivate() { }
        public virtual void OnDeactivate() { }
        public virtual void OnInitialize() { }
    }
}