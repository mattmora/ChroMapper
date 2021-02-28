using System.Globalization;
using UnityEngine;
using Zenject;

public class CoreInstaller : MonoInstaller
{
    [SerializeField] private GameObject sceneTransitionManagerPrefab;
    [SerializeField] private GameObject persistentUIPrefab;
    [SerializeField] private Localization loadingMessages;

    // Read https://github.com/svermeulen/Extenject documentation for what exactly is going on.
    public override void InstallBindings()
    {
        // Fixes weird shit regarding how people write numbers (20,35 VS 20.35), causing issues in JSON
        // This should be thread-wide, but I have this set throughout just in case it isnt.
        System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        Container.Bind<PluginLoader>().AsSingle().NonLazy();

        Container.BindInterfacesAndSelfTo<Settings>().AsSingle();
        Container.Bind<SceneTransitionManager>().FromComponentInNewPrefab(sceneTransitionManagerPrefab).AsSingle();
        Container.Bind<PersistentUI>().FromComponentInNewPrefab(persistentUIPrefab).AsSingle();

        Container.QueueForInject(loadingMessages);

        // Custom Platforms
        Container.BindInterfacesAndSelfTo<CustomPlatformSettings>().WhenInjectedInto<CustomPlatformsLoader>();
        Container.Bind<CustomPlatformsLoader>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
    }
}
