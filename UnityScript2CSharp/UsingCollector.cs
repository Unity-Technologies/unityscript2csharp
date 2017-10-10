using System;
using System.Collections.Generic;
using Boo.Lang.Compiler.Ast;
using Boo.Lang.Compiler.TypeSystem;
using Boo.Lang.Compiler.TypeSystem.Reflection;
using Attribute = Boo.Lang.Compiler.Ast.Attribute;

namespace UnityScript2CSharp
{
    internal class UsingCollector : DepthFirstVisitor
    {
        public UsingCollector()
        {
            Usings = new HashSet<string>();
        }

        public override void OnImport(Import node)
        {
            if ((node.Namespace.StartsWith("UnityEditor") || node.Namespace.StartsWith("UnityEngine")) && !_namespacesSeen.Contains(node.Namespace))
                return;

            Usings.Add(node.Namespace);
        }

        public override void OnCallableTypeReference(CallableTypeReference node)
        {
            var ns = typeof(Func<>).Namespace;
            Usings.Add(ns);
            _namespacesSeen.Add(ns);
        }

        public override void OnAttribute(Attribute node)
        {
            var ctor = node.Entity as IMember;
            if (ctor != null)
            {
                AddAsSeen(ctor.DeclaringType);
            }

            base.OnAttribute(node);
        }

        public override void OnSimpleTypeReference(SimpleTypeReference node)
        {
            AddAsSeen(node.Entity);
            base.OnSimpleTypeReference(node);
        }

        public override void OnGenericTypeReference(GenericTypeReference node)
        {
            AddAsSeen(node.Entity);
            base.OnGenericTypeReference(node);
        }

        private void AddAsSeen(IEntity entity)
        {

            var externalType = entity as ExternalType;
            if (externalType != null && externalType.ActualType != null)
            {
                _namespacesSeen.Add(externalType.ActualType.Namespace);
            }
        }

        public ISet<string> Usings { get; private set; }

        private ISet<string> _namespacesSeen = new HashSet<string>();
    }
}
