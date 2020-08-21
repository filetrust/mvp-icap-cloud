using DurableFileProcessing.Services;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Moq;
using NUnit.Framework;
using System;
using System.Text;

namespace DurableFileProcessing.Tests.Services
{
    public class BlobUtilitiesTests
    {
        public class GetSharedAccessSignatureMethod : BlobUtilitiesTests
        {
            [Test]
            public void GetSharedAccessSignature_WithoutBlob_Returns_The_Correct_Token()
            {
                // Arrange
                var dummyContainer = new CloudBlobContainer(
                    new Uri("http://test-storage.org"),
                    new StorageCredentials("fakeaccount",
                    Convert.ToBase64String(Encoding.Unicode.GetBytes("fakekeyval")), "fakekeyname"));

                var blobUtilities = new BlobUtilities();
                var permissions = SharedAccessBlobPermissions.Read;
                var expiryTime = DateTimeOffset.UtcNow.AddDays(1);

                // Act
                var result = blobUtilities.GetSharedAccessSignature(dummyContainer, expiryTime, permissions);

                // Assert
                Assert.That(result, Is.Not.Null);
            }

            [Test]
            public void GetSharedAccessSignature_WithBlob_Returns_The_Correct_Token()
            {
                // Arrange
                var dummyContainer = new CloudBlobContainer(
                    new Uri("http://test-storage.org"),
                    new StorageCredentials("fakeaccount",
                    Convert.ToBase64String(Encoding.Unicode.GetBytes("fakekeyval")), "fakekeyname"));

                var blobUtilities = new BlobUtilities();
                var permissions = SharedAccessBlobPermissions.Read;
                var expiryTime = DateTimeOffset.UtcNow.AddDays(1);

                // Act
                var result = blobUtilities.GetSharedAccessSignature(dummyContainer, "blob", expiryTime, permissions);

                // Assert
                Assert.That(result, Is.Not.Null);
            }
        }
    }
}
