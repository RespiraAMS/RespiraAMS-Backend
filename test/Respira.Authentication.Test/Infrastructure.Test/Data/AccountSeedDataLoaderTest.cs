using Respira.Authentication.Domain.Enums;
using Respira.Authentication.Infrastructure.Data;

namespace Respira.Authentication.Infrastructure.Test.Data;

public sealed class AccountSeedDataLoaderTest
{
    [Fact]
    public void Load_DefaultFilePath_ReturnsSeededAdminAccount()
    {
        var accounts = AccountSeedDataLoader.Load(AccountSeedDataLoader.DefaultFilePath);

        var admin = Assert.Single(accounts);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), admin.Id);
        Assert.Equal("admin@respira.ams", admin.Email);
        Assert.False(string.IsNullOrWhiteSpace(admin.HashPassword));
        Assert.Equal("0000000000", admin.Phone);
        Assert.Equal(RoleType.Admin, admin.Role);
        Assert.Equal(StatusType.Active, admin.Status);
        Assert.True(admin.IsEmailConfirmed);
        Assert.False(admin.IsDeleted);
        Assert.Null(admin.DeletedAt);
        Assert.Equal(TimeSpan.Zero, admin.CreatedAt.Offset);
        Assert.Equal(TimeSpan.Zero, admin.UpdatedAt.Offset);
    }

    [Fact]
    public void Load_NonExistentFile_ReturnsEmptyCollection()
    {
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"missing-seed-{Guid.CreateVersion7():N}.jsonc"
        );

        var accounts = AccountSeedDataLoader.Load(missingPath);

        Assert.Empty(accounts);
    }

    [Fact]
    public void Load_ValidJsoncWithCommentsAndMultipleRoles_MapsAndNormalizesFields()
    {
        var filePath = WriteTempSeedFile(
            """
            {
                // Seed accounts with comments and trailing commas
                "accounts": [
                    {
                        "id": "22222222-2222-2222-2222-222222222222",
                        "email": "  HOD.Doctor@Respira.AMS ",
                        "passwordHash": "$2a$12$hash1",
                        "phone": "0901112222",
                        "role": "HeadDepartmentDoctor",
                        "status": "Active",
                        "isEmailConfirmed": true,
                        "isDeleted": false,
                        "createdAt": "2025-06-01T07:00:00+07:00",
                        "updatedAt": "2025-06-01T07:00:00+07:00",
                    },
                    {
                        "id": "33333333-3333-3333-3333-333333333333",
                        "email": "reviewer@respira.ams",
                        "passwordHash": "$2a$12$hash2",
                        "phone": "0903334444",
                        "role": "MedicalRegulationReviewer",
                        "status": "Inactive",
                        "isEmailConfirmed": false,
                        "isDeleted": false
                    }
                ]
            }
            """
        );

        try
        {
            var accounts = AccountSeedDataLoader.Load(filePath);

            Assert.Equal(2, accounts.Count);

            var hod = accounts[0];
            Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), hod.Id);
            Assert.Equal("hod.doctor@respira.ams", hod.Email);
            Assert.Equal("$2a$12$hash1", hod.HashPassword);
            Assert.Equal("0901112222", hod.Phone);
            Assert.Equal(RoleType.HeadDepartmentDoctor, hod.Role);
            Assert.Equal(StatusType.Active, hod.Status);
            Assert.True(hod.IsEmailConfirmed);
            Assert.Equal(TimeSpan.Zero, hod.CreatedAt.Offset);
            Assert.Equal(new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero), hod.CreatedAt);

            var reviewer = accounts[1];
            Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), reviewer.Id);
            Assert.Equal("reviewer@respira.ams", reviewer.Email);
            Assert.Equal(RoleType.MedicalRegulationReviewer, reviewer.Role);
            Assert.Equal(StatusType.Inactive, reviewer.Status);
            Assert.False(reviewer.IsEmailConfirmed);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_UnknownRoleAndStatus_FallsBackToDoctorAndActive()
    {
        var filePath = WriteTempSeedFile(
            """
            {
                "accounts": [
                    {
                        "id": "44444444-4444-4444-4444-444444444444",
                        "email": "fallback@respira.ams",
                        "passwordHash": "$2a$12$hash",
                        "phone": "0905556666",
                        "role": "UnknownRole",
                        "status": "UnknownStatus"
                    }
                ]
            }
            """
        );

        try
        {
            var account = Assert.Single(AccountSeedDataLoader.Load(filePath));

            Assert.Equal(RoleType.Doctor, account.Role);
            Assert.Equal(StatusType.Active, account.Status);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_InvalidGuidId_ThrowsInvalidOperationException()
    {
        var filePath = WriteTempSeedFile(
            """
            {
                "accounts": [
                    {
                        "id": "not-a-guid",
                        "email": "bad@respira.ams",
                        "passwordHash": "$2a$12$hash"
                    }
                ]
            }
            """
        );

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                AccountSeedDataLoader.Load(filePath)
            );
            Assert.Contains("not-a-guid", ex.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_MissingEmail_ThrowsInvalidOperationException()
    {
        var filePath = WriteTempSeedFile(
            """
            {
                "accounts": [
                    {
                        "id": "55555555-5555-5555-5555-555555555555",
                        "email": "   ",
                        "passwordHash": "$2a$12$hash"
                    }
                ]
            }
            """
        );

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                AccountSeedDataLoader.Load(filePath)
            );
            Assert.Contains("missing its email address", ex.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_MissingPasswordHash_ThrowsInvalidOperationException()
    {
        var filePath = WriteTempSeedFile(
            """
            {
                "accounts": [
                    {
                        "id": "66666666-6666-6666-6666-666666666666",
                        "email": "nopass@respira.ams",
                        "passwordHash": ""
                    }
                ]
            }
            """
        );

        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                AccountSeedDataLoader.Load(filePath)
            );
            Assert.Contains("missing its password hash", ex.Message);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    private static string WriteTempSeedFile(string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"seed-accounts-test-{Guid.CreateVersion7():N}.jsonc"
        );
        File.WriteAllText(path, content);
        return path;
    }
}
