using System;
using Siege.Requisitions.Registrations;
using Siege.Requisitions.Resolution;

namespace Siege.Requisitions.RegistrationTemplates.Registration
{
    public class DefaultRegistrationTemplate : IRegistrationTemplate
    {
        public void Register(IServiceLocatorAdapter adapter, IRegistration registration, IResolutionTemplate template)
        {
            throw new NotImplementedException();
        }
    }
}