using System.Collections.Generic;
using EPiServer;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace Epinova.Associations
{
    internal class ContentEvents
    {
        public static void BindTwoWayRelationalContent(object sender, ContentEventArgs args)
        {
            //can't really do anything without a contentlink in the args...
            if (ContentReference.IsNullOrEmpty(args.ContentLink))
                return;

            var showstopper = ServiceLocator.Current.GetInstance<Showstopper>();
            if (showstopper.IsShowStoppedFor(args.Content.ContentLink.ID))
                return;

            var associationSourceContent = args.Content as IAssociationContent;
            if (associationSourceContent == null)
                return;

            var contentRepo = ServiceLocator.Current.GetInstance<IContentRepository>();
            var propertyWriter = ServiceLocator.Current.GetInstance<PropertyWriter>();
            var contentAssociationsHelper = ServiceLocator.Current.GetInstance<ContentInspector>();

            var currentContentVersion = contentRepo.Get<IAssociationContent>(new ContentReference(args.Content.ContentLink.ID, 0, args.Content.ContentLink.ProviderName, true));

            var associationProperties = contentAssociationsHelper.GetAssociationProperties(associationSourceContent);

            foreach (var property in associationProperties)
            {
                IEnumerable<ContentReference> associationRemovalTargets = contentAssociationsHelper.GetAssociationRemovalTargets(property, currentContentVersion, associationSourceContent);

                var propertyName = contentAssociationsHelper.GetAssociatedPropertyName(property);
                foreach (var associationRemovalTarget in associationRemovalTargets)
                    propertyWriter.RemoveAssociation(associationSourceContent, associationRemovalTarget, propertyName);
                
                IEnumerable<ContentReference> associationTargets = contentAssociationsHelper.GetAssociationTargets(property, associationSourceContent);

                foreach (var associationTarget in associationTargets)
                    propertyWriter.AddAssociation(associationSourceContent, associationTarget, propertyName);
            }

            showstopper.StartShow();
        }


        public static void RemoveRelationalContent(object sender, ContentEventArgs args)
        {
            // Only work magic on waste basket
            if (!args.TargetLink.Equals(ContentReference.WasteBasket))
                return;

            //can't really do anything without a contentlink in the args...
            if (ContentReference.IsNullOrEmpty(args.ContentLink))
                return;

            // Remove all elements in association properties and publish page. Publish event will handle the rest
            var contentRepository = ServiceLocator.Current.GetInstance<IContentRepository>();
            var content = contentRepository.Get<IContent>(args.ContentLink) as IReadOnly;

            var contentClone = content?.CreateWritableClone() as IAssociationContent;
            if(contentClone == null)
                return;

            var contentAssociationsHelper = ServiceLocator.Current.GetInstance<ContentInspector>();
            var associationProperties = contentAssociationsHelper.GetAssociationProperties(contentClone);

            foreach (var associationProperty in associationProperties)
            {
                contentClone.Property[associationProperty.Name].Clear();
            }

            contentRepository.Save(contentClone, SaveAction.Publish | SaveAction.SkipValidation, AccessLevel.NoAccess);
        }
    }
}