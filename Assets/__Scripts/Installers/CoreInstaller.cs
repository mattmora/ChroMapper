using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class CoreInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Read https://github.com/svermeulen/Extenject documentation for what exactly is going on.
        Container.Bind<Settings>().AsSingle().NonLazy();
    }
}
