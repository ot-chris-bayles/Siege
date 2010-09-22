/*   Copyright 2009 - 2010 Marcus Bratton

     Licensed under the Apache License, Version 2.0 (the "License");
     you may not use this file except in compliance with the License.
     You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

     Unless required by applicable law or agreed to in writing, software
     distributed under the License is distributed on an "AS IS" BASIS,
     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     See the License for the specific language governing permissions and
     limitations under the License.
*/

using Siege.Requisitions.Registrations.Stores;
using Siege.Requisitions.RegistrationTemplates;
using Siege.Requisitions.RegistrationTemplates.Named;

namespace Siege.Requisitions.Registrations.Named
{
    public class NamedInstanceRegistration<TBaseService> : InstanceRegistration<TBaseService>, INamedRegistration
    {
        private readonly string key;
        private readonly NamedInstanceRegistrationTemplate namedInstanceRegistrationTemplate = new NamedInstanceRegistrationTemplate();
        private readonly NamedRegistrationStore namedRegistrationStore = new NamedRegistrationStore();

        public NamedInstanceRegistration(string key)
        {
            this.key = key;
        }

        public string Key
        {
            get { return key; }
        }

        public override IRegistrationStore GetRegistrationStore()
        {
            return namedRegistrationStore;
        }

        public override IRegistrationTemplate GetRegistrationTemplate()
        {
            return namedInstanceRegistrationTemplate;
        }

        public override bool Equals(IRegistration registration)
        {
            if (!(registration is NamedRegistration<TBaseService>)) return false;

            var namedRegistration = registration as NamedRegistration<TBaseService>;

            return namedRegistration.Key == this.key && base.Equals(registration);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Key.GetHashCode();
        }
    }
}