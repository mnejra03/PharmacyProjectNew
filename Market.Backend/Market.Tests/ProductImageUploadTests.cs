using Market.API.Controllers;
using Market.Application.Abstractions;
using Market.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Market.Tests;

public sealed class ProductImageUploadTests
{
    [Fact]
    public async Task Customer_cannot_upload_product_image()
    {
        var storage = new RecordingFileStorage();
        var controller = CreateController(storage, new TestUser(isCustomer: true));

        var result = await controller.UploadImage(CreateFile("image/png", PngSignature), CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
        Assert.Equal(0, storage.SaveCount);
    }

    [Fact]
    public async Task Pharmacist_can_upload_image_with_matching_content_signature()
    {
        var storage = new RecordingFileStorage();
        var controller = CreateController(storage, new TestUser(isPharmacist: true));

        var result = await controller.UploadImage(CreateFile("image/png", PngSignature), CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(1, storage.SaveCount);
        Assert.Equal("image/png", storage.LastContentType);
    }

    [Fact]
    public async Task Mismatched_image_content_is_rejected_without_writing_to_storage()
    {
        var storage = new RecordingFileStorage();
        var controller = CreateController(storage, new TestUser(isAdmin: true));

        var result = await controller.UploadImage(CreateFile("image/png", "not an image"u8.ToArray()), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, storage.SaveCount);
    }

    private static ProductsController CreateController(RecordingFileStorage storage, IAppCurrentUser user)
    {
        var controller = new ProductsController(null!, storage, user);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private static IFormFile CreateFile(string contentType, byte[] content) =>
        new FormFile(new MemoryStream(content), 0, content.Length, "file", "test-image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];

    private sealed class TestUser(bool isAdmin = false, bool isPharmacist = false, bool isCustomer = false) : IAppCurrentUser
    {
        public int? UserId => 7;
        public string? Email => "test@example.com";
        public bool IsAuthenticated => true;
        public bool IsAdmin => isAdmin;
        public bool IsPharmacist => isPharmacist;
        public bool IsCustomer => isCustomer;
    }

    private sealed class RecordingFileStorage : IFileStorage
    {
        public int SaveCount { get; private set; }
        public string? LastContentType { get; private set; }
        public Task<StoredFile> SaveAsync(byte[] content, string contentType, string containerName, CancellationToken ct)
        {
            SaveCount++;
            LastContentType = contentType;
            return Task.FromResult(new StoredFile("test-image.png", "/images/test-image.png", contentType));
        }
        public Task<(byte[] Content, string ContentType)> ReadAsync(string key, CancellationToken ct) => throw new NotImplementedException();
    }
}
