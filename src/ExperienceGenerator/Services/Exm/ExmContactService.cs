﻿using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Models.Exm;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.DataAccess;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Analytics.Tracking;
using Sitecore.Data;
using ContactData = Sitecore.ListManagement.ContentSearch.Model.ContactData;

namespace ExperienceGenerator.Services.Exm
{
    public class ExmContactService
    {
        private readonly ExmDataPreparationModel _specification;
        private readonly string[] _languages = { "en", "uk" };
        private readonly Random _random = new Random();
        private readonly List<Contact> _contacts = new List<Contact>();

        public int ContactCount { get; private set; }

        public ExmContactService(ExmDataPreparationModel specification)
        {
            _specification = specification;
        }

        public void CreateContacts(int numContacts)
        {
            for (var i = 0; i < numContacts; i++)
            {
                CreateContact(i);
            }
        }

        public Contact GetContact(Guid id)
        {
            return _contacts.FirstOrDefault(x => x.ContactId == id);
        }

        public Contact CreateContact(int index)
        {
            var identifier = "XGen" + index;

            var contactRepository = new ContactRepository();

            var contact = contactRepository.LoadContactReadOnly(identifier);
            if (contact != null)
            {
                DoContactCreated(contact);
                return contact;
            }

            contact = contactRepository.CreateContact(ID.NewID);
            contact.Identifiers.AuthenticationLevel = AuthenticationLevel.None;
            contact.System.Classification = 0;
            contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
            contact.Identifiers.Identifier = "XGen" + index;
            contact.System.OverrideClassification = 0;
            contact.System.Value = 0;
            contact.System.VisitCount = 0;

            var contactPreferences = contact.GetFacet<IContactPreferences>("Preferences");
            contactPreferences.Language = _languages[index % _languages.Length];

            var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
            contactPersonalInfo.FirstName = Faker.Name.First();
            contactPersonalInfo.Surname = Faker.Name.Last();

            var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
            contactEmailAddresses.Entries.Create("Work").SmtpAddress =
                Faker.Internet.Email(string.Format("{0} {1}", contactPersonalInfo.FirstName, contactPersonalInfo.Surname));
            contactEmailAddresses.Preferred = "Work";

            var leaseOwner = new LeaseOwner("CONTACT_CREATE", LeaseOwnerType.OutOfRequestWorker);
            var options = new ContactSaveOptions(true, leaseOwner, null);
            contactRepository.SaveContact(contact, options);

            DoContactCreated(contact);
            return contact;
        }

        public List<ContactData> SelectRandomContacts(int min, int max)
        {
            var numberToTake = _random.Next(min, max);
            return SelectRandomContacts(numberToTake);
        }

        public List<ContactData> SelectRandomContacts(int numberToTake)
        {
            return _contacts
                .OrderBy(x => Guid.NewGuid())
                .Select(ContactToContactData)
                .Take(numberToTake)
                .ToList();
        }

        // TODO: Isn't this in Sitecore's API somewhere?
        public ContactData ContactToContactData(Contact contact)
        {
            var result = new ContactData
            {
                ContactId = contact.ContactId,
                Identifier = contact.Identifiers.Identifier
            };

            var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
            result.FirstName = contactPersonalInfo.FirstName;
            result.MiddleName = contactPersonalInfo.MiddleName;
            result.Surname = contactPersonalInfo.Surname;
            result.Nickname = contactPersonalInfo.Nickname;

            if (contactPersonalInfo.BirthDate != null)
            {
                result.BirthDate = contactPersonalInfo.BirthDate.Value;
            }

            result.Gender = contactPersonalInfo.Gender;
            result.JobTitle = contactPersonalInfo.JobTitle;
            result.Suffix = contactPersonalInfo.Suffix;
            result.Title = contactPersonalInfo.Title;

            var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
            result.PreferredEmail = contactEmailAddresses.Entries[contactEmailAddresses.Preferred].SmtpAddress;

            result.IdentificationLevel = contact.Identifiers.IdentificationLevel.ToString();
            result.Classification = contact.System.Classification;
            result.VisitCount = contact.System.VisitCount;
            result.Value = contact.System.Value;
            result.IntegrationLabel = contact.System.IntegrationLabel;

            return result;
        }

        private void DoContactCreated(Contact contact)
        {
            _contacts.Add(contact);
            _specification.Job.CompletedContacts++;
            ContactCount++;
        }
    }
}