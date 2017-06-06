using System.Collections.Generic;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Reflection;

namespace UnityScript2CSharp
{
    internal static class EntityExtensions
    {
        public static bool IsBoolean(this IEntity entity)
        {
            var typedEntity = (ITypedEntity)entity;
            if (typedEntity.Type.IsArray)
                return IsBoolean(((IArrayType)typedEntity.Type).ElementType);

            return typedEntity.Type.FullName == "boolean";
        }

        public static string DefaultValue(this IEntity entity)
        {
            var typedEntity = (ITypedEntity)entity;

            if (TypeSystemServices.IsReferenceType(typedEntity.Type))
                return "null";

            if (typedEntity.Type.FullName == "float")
                return "0.0f";

            return "0";
        }

        public static string TypeName(this IEntity entity, IList<string> usings)
        {
            string typeName = null;
            var externalType = entity as ExternalType;

            if (externalType != null)
            {
                switch (externalType.ActualType.FullName)
                {
                    case "System.String":
                        typeName = "string";
                        break;

                    case "System.Boolean":
                        typeName = "bool";
                        break;

                    case "System.Object":
                        typeName = "object";
                        break;

                    case "System.Int32":
                        typeName = "int";
                        break;

                    case "System.Int64":
                        typeName = "long";
                        break;
                }

                if (typeName == null && usings.Contains(externalType.ActualType.Namespace))
                {
                    typeName = externalType.Name;
                }
            }

            return typeName ?? entity.Name;
        }
    }
}
