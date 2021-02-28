using Zenject;

public class OptionsInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Neat way to bind all of the options without having to finaggle with prefabs and shit
        Container.Bind<SettingsBinder>().FromComponentInHierarchy().AsSingle();
    }
}
