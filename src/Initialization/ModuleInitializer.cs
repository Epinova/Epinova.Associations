using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Epinova.Associations.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(ServiceContainerInitialization))]
    public class ModuleInitializer : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
            IContentEvents events = ServiceLocator.Current.GetInstance<IContentEvents>();
            events.PublishingContent += ContentEvents.BindTwoWayRelationalContent;
            events.MovingContent += ContentEvents.RemoveRelationalContent;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Container.Configure(x => x.For<Showstopper>().Singleton());
        }
    }
}