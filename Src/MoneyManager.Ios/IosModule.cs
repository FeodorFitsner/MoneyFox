﻿using Autofac;
using MoneyManager.Foundation.Interfaces;

namespace MoneyManager.Ios
{
    public class IosModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<DialogService>().As<IDialogService>().SingleInstance();
            builder.RegisterType<AppInformation>().As<IAppInformation>().SingleInstance();
            builder.RegisterType<StoreFeatures>().As<IStoreFeatures>().SingleInstance();
            builder.RegisterType<RoamingSettings>().As<IRoamingSettings>().SingleInstance();
            builder.RegisterType<LocalSettings>().As<ILocalSettings>().SingleInstance();
        }
    }
}