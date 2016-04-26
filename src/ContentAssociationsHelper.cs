﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.Core;

namespace Epinova.Associations
{
    public class ContentAssociationsHelper
    {
        public static IEnumerable<PropertyInfo> GetAssociationProperties(IHasTwoWayRelation sourceRelationContent)
        {
            var contentType = sourceRelationContent.GetType();

            var contentProperties = contentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in contentProperties.Where(x => x.PropertyType == typeof (ContentArea) ||
                                                                  x.PropertyType == typeof (IList<ContentReference>)))
            {
                var associationAttribute = property.GetCustomAttributes(typeof (ContentAssociationAttribute)).FirstOrDefault() as ContentAssociationAttribute;
                if (associationAttribute == null)
                    continue;

                yield return property;
            }
        }

        public static IEnumerable<ContentReference> GetItemsToRemoveSourceFrom(PropertyInfo property, IHasTwoWayRelation currentlyPublishedVersion,
            IHasTwoWayRelation sourceRelationContent)
        {
            if (property.PropertyType == typeof (ContentArea))
            {
                var currentContent = property.GetValue(currentlyPublishedVersion) as ContentArea;
                var newContent = property.GetValue(sourceRelationContent) as ContentArea;

                if(currentContent == null)
                    currentContent = new ContentArea();

                var oldContentIds = currentContent.Items.Select(x => x.ContentLink.ID);
                var newContentIds = newContent?.Items.Select(x => x.ContentLink.ID) ?? Enumerable.Empty<int>(); 

                var itemsToRemoveFrom = oldContentIds.Except(newContentIds);

                return currentContent.Items
                    .Where(x => itemsToRemoveFrom.Contains(x.ContentLink.ID))
                    .Select(x => x.ContentLink);
            }

            if (property.PropertyType == typeof (IList<ContentReference>))
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

            throw new Exception("Attempt to use property on unsupported property. Currently, ContentArea and IList<ContentArea> is supported");
        }

        public static IEnumerable<ContentReference> GetItemsToAddAssociationTo(PropertyInfo property, IHasTwoWayRelation sourceRelationContent)
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
            throw new Exception("Attempt to use property on unsupported property. Currently, ContentArea and IList<ContentArea> is supported");
        }
    }
}