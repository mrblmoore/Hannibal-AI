using System;
using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade
{
    /// <summary>
    /// Base class for all Mount & Blade II: Bannerlord mods
    /// </summary>
    public abstract class MBSubModuleBase
    {
        /// <summary>
        /// Called when the game first loads.
        /// </summary>
        protected virtual void OnSubModuleLoad() { }
        
        /// <summary>
        /// Called after the game engine has been initialized.
        /// </summary>
        protected virtual void OnBeforeInitialModuleScreenSetAsRoot() { }
        
        /// <summary>
        /// Called when the game first starts, before the main menu.
        /// </summary>
        protected virtual void OnGameStart(Game game, IGameStarter gameStarter) { }
        
        /// <summary>
        /// Called when the player enters the game world.
        /// </summary>
        protected virtual void OnGameLoaded(Game game, object initializerObject) { }
        
        /// <summary>
        /// Called every frame in the game.
        /// </summary>
        /// <param name="dt">Delta time in seconds since last frame.</param>
        protected virtual void OnApplicationTick(float dt) { }
        
        /// <summary>
        /// Called when a new game state is being created.
        /// </summary>
        public virtual void OnGameInitializationFinished(Game game) { }
        
        /// <summary>
        /// Called when the game is exiting.
        /// </summary>
        protected virtual void OnSubModuleUnloaded() { }
        
        /// <summary>
        /// Called when a mission is being initialized, allows adding mission behaviors.
        /// </summary>
        public virtual void OnMissionBehaviorInitialize(Mission mission) { }
    }
    
    /// <summary>
    /// Interface for starting a game.
    /// </summary>
    public interface IGameStarter
    {
        void AddModel(GameModel gameModel);
        void AddBehavior(MBBehavior behavior);
    }
    
    /// <summary>
    /// Base class for game behaviors.
    /// </summary>
    public abstract class MBBehavior
    {
        public abstract string Id { get; }
        
        public virtual void RegisterEvents() { }
        public virtual void SyncData(object sync) { }
    }
    
    /// <summary>
    /// Base class for game models.
    /// </summary>
    public abstract class GameModel
    {
        public abstract string Id { get; }
    }
}