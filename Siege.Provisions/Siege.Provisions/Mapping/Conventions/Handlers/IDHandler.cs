﻿using System;
using System.Reflection;

namespace Siege.Provisions.Mapping.Conventions.Handlers
{
    public class IDHandler : IHandler
    {
        private readonly Predicate<PropertyInfo> idMatcher;

        public IDHandler(Predicate<PropertyInfo> idMatcher)
        {
            this.idMatcher = idMatcher;
        }

        public bool CanHandle(PropertyInfo property)
        {
            return this.idMatcher(property);
        }

        public void Handle(PropertyInfo property, Type type, DomainMapping mapper)
        {
            mapper.MapID(property);
        }
    }
}