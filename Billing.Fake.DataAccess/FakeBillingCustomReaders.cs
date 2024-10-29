using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Billing.Data.Models;
using Billing.DataAccess;
using DataAccess.Shared;

namespace Billing.Fake.DataAccess; 

public sealed class FakeFacilityReader : FakeBillingReader<Facility>, IFacilityReader {
    public FakeFacilityReader(FakeBillingDataFactory billingDataFactory) : base(billingDataFactory) { }

    public Task<IEnumerable<Provider>> GetFacilityProvidersAsync(IdPk facilityId) => Task.FromResult( fakeBillingDataFactory.GetItems<Provider>().AsEnumerable());
}

public sealed class FakeProviderReader : FakeBillingReader<Provider>, IProviderReader {
    public FakeProviderReader(FakeBillingDataFactory billingDataFactory) : base(billingDataFactory) { }
    public Task<ProviderWithFacilities?> GetWithFacilitiesAsync(IdPk providerId) => Task.FromResult( fakeBillingDataFactory.GetItems<ProviderWithFacilities>().FirstOrDefault());

    public Task<IEnumerable<Facility>> GetProviderFacilitiesAsync(IdPk providerId) => Task.FromResult( fakeBillingDataFactory.GetItems<Facility>().AsEnumerable());
}

public sealed class FakePatientReader : FakeBillingReader<Patient>, IPatientReader {
    public FakePatientReader(FakeBillingDataFactory billingDataFactory) : base(billingDataFactory) { }

    public Task<IEnumerable<ServiceLocation>> GetPatientServiceLocationsAsync(IdPk patientId) => Task.FromResult( fakeBillingDataFactory.GetItems<ServiceLocation>().AsEnumerable());
    public Task<IEnumerable<Policy>> GetPatientPolicyAsync(long patientId, long insuranceCompanyId, string policyNumber) {
        throw new System.NotImplementedException();
    }
}

public sealed class FakeBillableServiceReader : FakeBillingReader<BillableService>, IBillableServiceReader {
    public FakeBillableServiceReader(FakeBillingDataFactory billingDataFactory) : base(billingDataFactory) { }

    public Task<IEnumerable<BillableServiceWithLookups>> GetBillableServicesWithLookupsAsync(Filter filter) {
        return Task.FromResult( fakeBillingDataFactory.GetItems<BillableServiceWithLookups>().AsEnumerable());
    }
}