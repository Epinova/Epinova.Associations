using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.Core;

namespace Epinova.Associations
{
    internal class ContentInspector
    {
        private readonly Type[] _supportedTypes = { typeof(ContentReference), typeof (ContentArea), typeof (IList<ContentReference>) };

        public IEnumerable<PropertyInfo> GetAssociationProperties(IAssociationContent sourceRelationContent)
        {
            var contentType = sourceRelationContent.GetType();
            var contentProperties = contentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in contentProperties.Where(x => _supportedTypes.Contains(x.PropertyType)))
            {
                if(property.GetCustomAttributes<ContentAssociationAttribute>().Any())
                    yield return property;
            }
        }

        public IEnumerable<ContentReference> GetAssociationRemovalTargets(PropertyInfo property, IAssociationContent currentlyPublishedVersion, IAssociationContent sourceRelationContent)
        {
            if (property.PropertyType == typeof (ContentArea))
            {
                return GetContentAreaRemovalTargets(property, currentlyPublishedVersion, sourceRelationContent);
            }
            if (property.PropertyType == typeof (IList<ContentReference>))
            {
                return GetContentReferenceListRemovalTargets(property, currentlyPublishedVersion, sourceRelationContent);
            }
            if (property.PropertyType == typeof (ContentReference))
            {
                return GetContentReferenceRemovalTargets(property, currentlyPublishedVersion, sourceRelationContent);
            }

            throw new Exception("Attempt to use property on unsupported property. Currently, ContentReference, ContentArea and IList<ContentReference> is supported");
        }

        private IEnumerable<ContentReference> GetContentReferenceRemovalTargets(PropertyInfo property, IAssociationContent currentlyPublishedVersion, IAssociationContent sourceRelationContent)
        {
            var currentContent = property.GetValue(currentlyPublishedVersion) as ContentReference;
            var newContent = property.GetValue(sourceRelationContent) as ContentReference;

            if (currentContent == null)
                return Enumerable.Empty<ContentReference>();

            if(currentContent.ID == newContent?.ID)
                return Enumerable.Empty<ContentReference>();

            return new List<ContentReference>() {currentContent};
        }

        private IEnumerable<ContentReference> GetContentReferenceListRemovalTargets(PropertyInfo property, IAssociationContent currentlyPublishedVersion, IAssociationContent sourceRelationContent)
        {
            var currentContent = property.GetValue(currentlyPublishedVersion) as IList<ContentReference>;
            var newContent = property.GetValue(sourceRelationContent) as IList<ContentReference>;

            if (currentContent == null)
                currentContent = new List<ContentReference>();

            var oldContentIds = currentContent.Select(x => x.ID);
            var newContentIds = newContent?.Select(x => x.ID) ?? Enumerable.Empty<int>();

            var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

            return currentContent.Where(x => itemsToRemoveFrom.Contains(x.ID));
        }

        private IEnumerable<ContentReference> GetContentAreaRemovalTargets(PropertyInfo property, IAssociationContent currentlyPublishedVersion, IAssociationContent sourceRelationContent)
        {
            var currentContent = property.GetValue(currentlyPublishedVersion) as ContentArea;
            var newContent = property.GetValue(sourceRelationContent) as ContentArea;

            if (currentContent == null)
                currentContent = new ContentArea();

            var oldContentIds = currentContent.Items.Select(x => x.ContentLink.ID);
            var newContentIds = newContent?.Items.Select(x => x.ContentLink.ID) ?? Enumerable.Empty<int>();

            var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

            return currentContent.Items
                                 .Where(x => itemsToRemoveFrom.Contains(x.ContentLink.ID))
                                 .Select(x => x.ContentLink);
        }

        public IEnumerable<ContentReference> GetAssociationTargets(PropertyInfo property, IAssociationContent sourceRelationContent)
        {
            if (property.PropertyType == typeof (ContentArea))
            {
                var contentArea = property.GetValue(sourceRelationContent) as ContentArea;
                if (contentArea == null)
                    return Enumerable.Empty<ContentReference>();

                return contentArea.Items.Select(x => x.ContentLink);
            }

            if (property.PropertyType == typeof (IList<ContentReference>))
            {
                var contentRefList = property.GetValue(sourceRelationContent) as IList<ContentReference>;

                return contentRefList ?? Enumerable.Empty<ContentReference>();
            }

            if (property.PropertyType == typeof (ContentReference))
            {
                var contentRef = property.GetValue(sourceRelationContent) as ContentReference;

                return contentRef == null ? Enumerable.Empty<ContentReference>() : new List<ContentReference>() {contentRef};
            }

            throw new Exception("Attempt to use property on unsupported property. Currently, ContentReference, ContentArea and IList<ContentReference> is supported");
        }

        public string GetAssociatedPropertyName(PropertyInfo property)
        {
            var attribute = Attribute.GetCustomAttribute(property, typeof(ContentAssociationAttribute)) as ContentAssociationAttribute;
            if (string.IsNullOrEmpty(attribute?.AssociatedPropertyName))
                return property.Name;
            
            return attribute.AssociatedPropertyName;
        }
    }
}