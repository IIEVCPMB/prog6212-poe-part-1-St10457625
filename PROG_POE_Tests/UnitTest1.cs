using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Models;
using PROG_POE_Part_1.Services;
using System.Text;
using Xunit;

namespace PROG_POE_Part_1.Tests
{
    public class LMSTest
    {
        [Fact]
        public void Test1_AddBook_Succes()
        {
            //Create a new Book
            var initialCount = ClaimService.GetAllClaimsAsync().Count;

            var newClaim = new Claim
            {
                Claim_ID = 4,
                Lecturer_ID = 23895,
                Name = "Nathan Drake",
                Date_Submitted = DateTime.Now.AddDays(-7),
                Total_Hours = 15,
                Hourly_Rate = 200,
                Status = Status.Pending,
                Documents = new List<UploadedDocument>(),
                Reviews = new List<ClaimReview>()
            };

            //perform the action
            ClaimData.AddClaim(newClaim);

            // get the new count
            var newCount = ClaimData.GetAllClaims().Count();
            Assert.Equal(initialCount + 1, newCount);

            Assert.True(newClaim.Claim_ID > 0, "Claim should have an ID assigned");

            Assert.Equal(Status.Pending, newClaim.Status);

            //Verify if we can retrieve the book
            var retrievedClaim = ClaimData.GetClaimByID(newClaim.Claim_ID);
            Assert.NotNull(retrievedClaim);
            Assert.Equal("Nathan Drake", retrievedClaim.Name);


        }

        [Fact]
        public async Task Test2_EncryptionFile_Successful()
        {
            var originalContent = "this is a secret file content that should be encrypted";
            var originalBytes = Encoding.UTF8.GetBytes(originalContent);
            var inputStream = new MemoryStream(originalBytes);
            var tempFile = Path.GetTempFileName();
            var encryptionService = new FileEncryptionService();

            try
            {
                await encryptionService.EncryptFileAsync(inputStream, tempFile);

                Assert.True(File.Exists(tempFile), "Encrypted file should exist");

                //Read the encrypted file
                var encryptedBytes = await File.ReadAllBytesAsync(tempFile);

                //Verify that the encrypted data is not the same as the original
                Assert.NotEqual(originalBytes, encryptedBytes);

                //verify the encrypted 
                Assert.True(encryptedBytes.Length > 0, "Encrypted file should have content");

                //Verify we cannot read the original text from the encrypted file
                var encryptedText = Encoding.UTF8.GetString(encryptedBytes);
                Assert.DoesNotContain("This is a secret file content that should be encrypted", encryptedText);
            }
            finally
            {

                if (File.Exists(tempFile)) File.Delete(tempFile);
            }

        }

        [Fact]
        public async Task test3_DecryptionFile_Succesful()
        {
            //Create and encrypt the file
            var origialContent = "this is a secret document";
            var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(origialContent));
            var tempFile = Path.GetTempFileName();
            var encryptionService = new FileEncryptionService();

            try
            {
                //Encrypt
                await encryptionService.EncryptFileAsync(inputStream, tempFile);

                //Decrypt
                var decryptedStream = await encryptionService.DecryptFileAsync(tempFile);
                var decryptedContent = Encoding.UTF8.GetString(decryptedStream.ToArray());

                //Verify decrypted content matched the kelz:)
                Assert.Equal(origialContent, decryptedContent);
                Assert.Contains(origialContent, decryptedContent);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void Test4_ApproveBook()
        {
            var newClaim = new Claim
            {
                Claim_ID = 5,
                Lecturer_ID = 23895,
                Name = "Drake Luke",
                Date_Submitted = DateTime.Now.AddDays(-10),
                Total_Hours = 10,
                Hourly_Rate = 150,
                Status = Status.Approved,
                Documents = new List<UploadedDocument>(),
                Reviews = new List<ClaimReview>()
            };

            ClaimData.AddClaim(newClaim);

            //Action : perform book approve
            var success = ClaimData.UpdateClaimStatus(newClaim.Claim_ID, Status.Approved, "Manager", "Claim approved successfully");

            Assert.True(success, "Update should succeed");

            var updateClaim = ClaimData.GetClaimByID(newClaim.Claim_ID);
            Assert.Equal(Status.Approved, updateClaim.Status);
            Assert.Equal("Manager", updateClaim.ReviewedBy);
            Assert.NotNull(updateClaim.ReviewedDate);
        }

        [Fact]
        public void Test5_DeclineBook()
        {

            var newClaim = new Claim
            {
                Claim_ID = 8,
                Lecturer_ID = 89124,
                Name = "Jaden Smith",
                Date_Submitted = DateTime.Now.AddDays(-3),
                Total_Hours = 15,
                Hourly_Rate = 250,
                Status = Status.Declined,
                Documents = new List<UploadedDocument>(),
                Reviews = new List<ClaimReview>()
            };

            ClaimData.AddClaim(newClaim);

            //Action : perform book approve
            var decline = ClaimData.UpdateClaimStatus(newClaim.Claim_ID, Status.Declined, "Manager", "Claim declined");

            Assert.True(decline, "Update should succeed");

            var updateClaim = ClaimData.GetClaimByID(newClaim.Claim_ID);
            Assert.Equal(Status.Declined, updateClaim.Status);
            Assert.Equal("Manager", updateClaim.ReviewedBy);
            Assert.NotNull(updateClaim.ReviewedDate);

        }
    }
}