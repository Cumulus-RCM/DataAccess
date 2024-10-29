using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BaseLib;
using Billing.Data.Enums;
using Billing.Data.Interfaces;
using Billing.Data.Models;
using Bogus;
using Bogus.Healthcare;

namespace Billing.Fake.DataAccess {
    public class FakeBillingDataFactory(int collectionSize = 10) {
        private static readonly Dictionary<Type, IList> dictionary = new ();

        static FakeBillingDataFactory() {
            Bogus.Premium.License.LicenseTo = "Avi Stokar";
            Bogus.Premium.License.LicenseKey =
                "d9UDxVOa5wPn1WViTZWnP+hQhu5QJ/CcdAs4jp4icxEWo/+vdrUmT1dEbRzYdA4fd/IKmc2j5s3YLNQzi0eongKgcdrMA/03AI3l+hNlaWKBf4sXApoVb0drkneYebhMZFO74hsjtHNY1PaxCMDMeQO1L2wXsE80IBAeQyOK6E0=";
        }

        public List<AppUser> GetMockAppUsers() => 
            GetItems<Provider>().Select(p => new AppUser {Id = p.Id, UserName = p.FirstName + p.LastName}).ToList();

        public List<T> GetItems<T>() where T : class {
            if (dictionary.TryGetValue(typeof(T), out var items)) return (List<T>)items;
            if (typeof(T) == typeof(AppUser)) return (GetMockAppUsers() as List<T>)!;

            var faker = createFaker<T>();
            var generatedItems = faker.Generate(collectionSize);
             if (typeof(T) == typeof(Facility)) {
                var serviceLocations = generatedItems.Select(f=> f as Facility).Select(f => new ServiceLocation {
                    Id = f!.Id,
                    Description = f.Description,
                    Address = new Address(f),
                    IsFacility = true
                }).ToList();
                dictionary.Add(typeof(ServiceLocation), serviceLocations);
            }

            dictionary.Add(typeof(T), generatedItems);
            return generatedItems;
        }

        private Faker<T> createFaker<T>() where T : class {
            var faker = new Faker<T>().StrictMode(false);
            faker.Rules(getRules<T>());
            return faker;
        }

        private Action<Faker, T>? getRules<T>() where T : class {
            var type = typeof(T);
            if (type == typeof(Patient)) return (Action<Faker, T>?)patientRules;
            if (type == typeof(Practice)) return (Action<Faker, T>?)practiceRules;
            if (type == typeof(PatientPhone)) return (Action<Faker, T>)patientPhoneRules();
            if (type == typeof(PatientAddress)) return (Action<Faker, T>?)patientAddressRules;
            if (type == typeof(Policy)) return (Action<Faker, T>?)policyRules;
            if (type == typeof(InsuranceCompany)) return (Action<Faker, T>?)insuranceCompanyRules;
            if (type == typeof(Provider)) return (Action<Faker, T>?)providerRules;
            if (type == typeof(Facility)) return (Action<Faker, T>?)facilityRules;
            if (type == typeof(BillableService)) return (Action<Faker, T>?)billableServiceRules;
            if (type == typeof(CptCode)) return (Action<Faker, T>?)cptCodeRules;
            if (type == typeof(AppUser)) return (Action<Faker, T>?)appUserRules;
            if (type == typeof(Diagnosis)) return (Action<Faker, T>?)diagnosisRules;
            if (type == typeof(BillableServiceWithLookups)) return (Action<Faker, T>?)billableServiceWithLookupsRules;
            return null;
        }

        private static Action<Faker, Practice> practiceRules => (faker, practice) => {
            practice.Id = faker.Random.Long(1,100);
            practice.CompanyName = faker.Company.CompanyName();
            practice.EIN = faker.Random.Long(100000000, 999999999).ToString();
            practice.Website = faker.Internet.Url();
        };

        private Action<Faker, BillableServiceWithLookups> billableServiceWithLookupsRules => (faker, service) => {
            service.BillableService = faker.PickRandom(GetItems<BillableService>());
            service.Patient = faker.PickRandom(GetItems<Patient>());
            service.ServiceLocation = new ServiceLocation(faker.PickRandom(GetItems<PatientAddress>()));
            service.Provider = faker.PickRandom(GetItems<Provider>());
            service.CptCode = faker.PickRandom(GetItems<CptCode>());
            service.Diagnoses = faker.PickRandom<Diagnosis>(GetItems<Diagnosis>(), 3).ToList();
        };

        private static Action<Faker, AppUser> appUserRules => (faker, appUser) => {
            appUser.Id = faker.Random.Long(1,100);
            appUser.UserName = faker.Internet.Email();
        };

        private static Action<Faker, CptCode> cptCodeRules => (faker, cptCode) => {
            cptCode.Id = faker.Random.Long(1,100);
            cptCode.Code = faker.Random.String2(5, "0123456789");
            cptCode.Description = faker.Human().Diagnosis();
        };

        private static Action<Faker, Diagnosis> diagnosisRules => (faker, diagnosis) => {
            diagnosis.Id = faker.Random.Long(1,100);
            diagnosis.IcdCode = faker.Random.String2(5, "0123456789");
            diagnosis.Description = faker.Human().Procedure();
        };

        private static Action<Faker, Facility> facilityRules => (f, fa) => {
            fa.Id = f.Random.Long(1,100);
            fa.Description = f.Company.CompanyName();
            fa.Street1 = f.Address.StreetAddress();
            fa.Street2 = f.Address.SecondaryAddress();
            fa.City = f.Address.City();
            fa.State = f.Address.StateAbbr();
            fa.ZipCode = f.Address.ZipCode();
        };

        private Action<Faker, BillableService> billableServiceRules =>
            (f, bs) => {
                var patient = f.PickRandom(GetItems<Patient>());
                var cptCode = f.PickRandom(GetItems<CptCode>());
                var diagnoses = f.PickRandom<Diagnosis>(GetItems<Diagnosis>(), 3).ToArray();
                bs.Id = f.Random.Long(1, 100000);
                bs.ServiceDate = DateTime.Today;
                bs.Patient_Id = patient.Id;
                bs.Provider_Id = f.PickRandom(GetItems<Provider>().Select(p => p.Id));
                bs.Units = 1;
                bs.Service_Facility_Id = f.PickRandom(GetItems<Facility>().Select(fa => fa.Id));
                bs.Cpt_Code = cptCode.Code;
                bs.IcdCodes = $"{diagnoses[0].IcdCode},{diagnoses[1].IcdCode},{diagnoses[2].IcdCode}";
            };

        private Action<Faker, Provider> providerRules => (f, p) => {
            p.Id = f.Random.Long(1,100);
            p.LastName = f.Person.LastName;
            p.FirstName = f.Person.FirstName;
            p.Npi = f.Random.Long(1000000000, 9999999999).ToString();
            p.CptFavorites = string.Join(",", f.PickRandom<CptCode>(GetItems<CptCode>(),3).Select(cpt => cpt.Code));
            p.DiagnosisFavorites = string.Join(",", f.PickRandom<Diagnosis>(GetItems<Diagnosis>(),3).Select(cpt => cpt.IcdCode));
        };

        private Action<Faker, Patient> patientRules => (f, p) => {
            p.Id = f.Random.Long(1,100);
            p.LastName = f.Person.LastName;
            p.FirstName = f.Person.FirstName;
            p.Gender = f.Person.Gender == 0 ? "M" : "F";
            p.Birthdate = f.Date.PastDateOnly(25);
            p.DeathDate = null;
            p.Facility_Id = f.PickRandom(GetItems<Facility>().Select(fa => fa.Id));
        };

        private Action<Faker, PatientPhone> patientPhoneRules() => (f, pp) => {
            pp.Id = f.PickRandom(GetItems<Patient>()).Id;
            pp.Patient_Id = f.Random.Long();
            pp.PhoneNumber = f.Phone.PhoneNumber();
            pp.PhoneKind = (char)f.PickRandom(Enumeration.GetAll<PhoneKind>());
        };


        private Action<Faker, PatientAddress> patientAddressRules => (f, pa) => {
            pa.Id = f.Random.Long(1);
            pa.AddressKind = (char)f.PickRandom(Enumeration.GetAll<AddressKind>());
            pa.Patient_Id = f.PickRandom(GetItems<Patient>()).Id;
            pa.Street1 = f.Address.StreetAddress();
            pa.Street2 = f.Address.SecondaryAddress();
            pa.City = f.Address.City();
            pa.State = f.Address.StateAbbr();
            pa.ZipCode = f.Address.ZipCode();
            pa.Priority = f.Random.Short(1, 10);
        };

        private Action<Faker, Policy> policyRules => (f, p) => {
            p.Id = f.Random.Long(1);
            p.Patient_Id = f.PickRandom(GetItems<Patient>()).Id;
            p.Priority = f.Random.Short(1, 10);
            p.InsuranceCompany_Id = f.PickRandom(GetItems<InsuranceCompany>().Select(ic => ic.Id));
            p.PolicyNumber = f.Vehicle.Vin();
            p.ActiveDate = f.Date.PastDateOnly(5);
        };

        private static Action<Faker, InsuranceCompany> insuranceCompanyRules => (f, ic) => {
            ic.Id = f.Random.Long(1);
            ic.CompanyName = f.Company.CompanyName();
        };
    }
}