using System;

namespace Epinova.Associations
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ContentAssociationAttribute : Attribute
    {
        public string AssociatedPropertyName { get; set; }
    }
}