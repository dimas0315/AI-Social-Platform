﻿using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using AI_Social_Platform.Data;
using AI_Social_Platform.Services.Data;
using AI_Social_Platform.Services.Data.Models.PublicationDtos;

using static AI_Social_Platform.Common.ExceptionMessages.PublicationExceptionMessages;

namespace Ai_Social_Platform.Tests
{
    [TestFixture]
    public class PublicationServiceTest
    {
        private DbContextOptions<ASPDbContext> options;
        private ASPDbContext dbContext;
        private PublicationService publicationService;
        private HttpContextAccessor httpContextAccessor;

        [SetUp]
        public void Setup()
        {
            this.options = new DbContextOptionsBuilder<ASPDbContext>()
                .UseInMemoryDatabase("ASPInMemory" + Guid.NewGuid())
                .Options;

            dbContext = new ASPDbContext(options);

            this.dbContext.Database.EnsureCreated();

            DatabaseSeeder.SeedDatabase(dbContext);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "123400ce-d726-4fc8-83d9-d6b3ac1f591e")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = user
            };

            httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = httpContext
            };

            publicationService = new PublicationService(dbContext, httpContextAccessor);
        }

        [TearDown]
        public void OneTimeTearDown()
        {
            dbContext.Dispose();
        }

        [Test]
        public async Task GetPublicationsAsync_ReturnsPublications()
        {
            // Act
            var result = await publicationService.GetPublicationsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<IEnumerable<PublicationDto>>(result);

            // Assuming some publications were seeded during setup
            Assert.IsTrue(result.Any());
        }

        [Test]
        public async Task GetPublicationAsync_ValidId_ReturnsPublicationDto()
        {
            // Arrange - Assuming there's a publication with a known ID in the seeded data
            var knownPublicationId = dbContext.Publications.First().Id;

            // Act
            var result = await publicationService.GetPublicationAsync(knownPublicationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<PublicationDto>(result);
            Assert.That(result.Id, Is.EqualTo(knownPublicationId));
        }

        [Test]
        public async Task GetPublicationAsync_InvalidId_ThrowsNullReferenceException()
        {
            // Arrange - Assuming there's an invalid publication ID
            var invalidPublicationId = Guid.NewGuid(); // Use a non-existing ID

            // Act and Assert
            var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await publicationService.GetPublicationAsync(invalidPublicationId);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(PublicationNotFound));
        }

        [Test]
        public async Task GetPublicationAsync_ValidId_ReturnsPublicationDtoWithSameIdAndContent()
        {
            //Arrange - Assuming there's a publication with a known ID in the seeded data
            var knownPublication = dbContext.Publications.First();

            // Act
            var result = await publicationService.GetPublicationAsync(knownPublication.Id);

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<PublicationDto>(result);
            Assert.That(result.Id, Is.EqualTo(knownPublication.Id));
            Assert.That(result.Content, Is.EqualTo(knownPublication.Content));
            Assert.That(result.DateCreated, Is.EqualTo(knownPublication.DateCreated));
            Assert.That(result.AuthorId, Is.EqualTo(knownPublication.AuthorId));
        }

        [Test]
        public async Task CreatePublicationAsync_ValidDto_CreatesPublication()
        {
            // Arrange
            var dto = new PublicationFormDto()
            {
                Content = "This is a test publication"
            };
            var countBefore = dbContext.Publications.Count();

            // Act
            await publicationService.CreatePublicationAsync(dto);

            // Assert
            Assert.That(dbContext.Publications.Count(), Is.EqualTo(countBefore + 1));
        }

        [Test]
        public async Task CreatePublicationAsync_ValidDto_CreatesPublicationWithCorrectContent()
        {
            // Arrange
            var dto = new PublicationFormDto()
            {
                Content = "This is a test publication"
            };

            // Act
            await publicationService.CreatePublicationAsync(dto);

            // Assert
            Assert.That(dbContext.Publications.Last().Content, Is.EqualTo(dto.Content));
        }

        [Test]
        public async Task CreatePublicationAsync_ValidDto_CreatesPublicationWithCorrectAuthorId()
        {
            // Arrange
            var dto = new PublicationFormDto()
            {
                Content = "This is a test publication"
            };
            var userId = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            // Act
            await publicationService.CreatePublicationAsync(dto);

            // Assert
            Assert.That(dbContext.Publications.Last().AuthorId.ToString(), Is.EqualTo(userId));
        }

        [Test]
        public async Task UpdatePublicationAsync_ValidDtoAndId_UpdatesPublication()
        {
            // Arrange
            var dto = new PublicationFormDto()
            {
                Content = "This is a test publication"
            };
            var publication =
                dbContext.Publications.First(p => p.AuthorId.ToString() == "123400ce-d726-4fc8-83d9-d6b3ac1f591e");

            // Act
            await publicationService.UpdatePublicationAsync(dto, publication.Id);

            // Assert
            Assert.That(publication.Content, Is.EqualTo(dto.Content));
        }

        [Test]
        public async Task UpdatePublicationAsync_ThrowsNullReferenceException()
        {
            // Arrange
            var dto = new PublicationFormDto()
            {
                Content = "This is a test publication"
            };
            var invalidPublicationId = Guid.NewGuid(); // Use a non-existing ID

            // Act and Assert
            var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await publicationService.UpdatePublicationAsync(dto, invalidPublicationId);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(PublicationNotFound));
        }


        [Test]
        public async Task UpdatePublicationAsync_ThrowsAccessViolationExceptionException()
        {
            // Arrange
            var dto = new PublicationFormDto()
            {
                Content = "This is a test publication"
            };
            var publication =
                dbContext.Publications.First(p => p.AuthorId.ToString() == "949a14ed-2e82-4f5a-a684-a9c7e3ccb52e");

            // Act and Assert
            var exception = Assert.ThrowsAsync<AccessViolationException>(async () =>
            {
                await publicationService.UpdatePublicationAsync(dto, publication.Id);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(NotAuthorizedToEditPublication));
        }

        [Test]
        public async Task DeletePublicationAsync_ValidId_DeletesPublication()
        {
            // Arrange
            var publication =
                dbContext.Publications.First(p => p.AuthorId.ToString() == "123400ce-d726-4fc8-83d9-d6b3ac1f591e");
            var countBefore = dbContext.Publications.Count();

            // Act
            await publicationService.DeletePublicationAsync(publication.Id);

            // Assert
            Assert.That(dbContext.Publications.Count(), Is.EqualTo(countBefore - 1));
        }

        [Test]
        public Task DeletePublicationAsync_ThrowsNullReferenceException()
        {
            // Arrange
            var invalidPublicationId = Guid.NewGuid(); // Use a non-existing ID

            // Act and Assert
            var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await publicationService.DeletePublicationAsync(invalidPublicationId);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(PublicationNotFound));
            return Task.CompletedTask;
        }

        [Test]
        public async Task DeletePublicationAsync_ThrowsAccessViolationExceptionException()
        {
            // Arrange
            var publication =
                dbContext.Publications.First(p => p.AuthorId.ToString() == "949a14ed-2e82-4f5a-a684-a9c7e3ccb52e");

            // Act and Assert
            var exception = Assert.ThrowsAsync<AccessViolationException>(async () =>
            {
                await publicationService.DeletePublicationAsync(publication.Id);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(NotAuthorizedToDeletePublication));
        }

        [Test]
        public async Task DeletePublicationAsync_DeletesComments()
        {
            // Arrange
            var publicationId = dbContext.Publications.Last().Id;

            // Act
            await publicationService.DeletePublicationAsync(publicationId);

            // Assert
            Assert.IsEmpty(dbContext.Comments.Where(c => c.PublicationId == publicationId));
        }

        [Test]
        public async Task CreateCommentAsync_ValidDto_CreatesComment()
        {
            // Arrange
            var dto = new CommentFormDto()
            {
                Content = "This is a test comment"
            };
            var publicationId = dbContext.Publications.First().Id;
            var countBefore = dbContext.Comments.Count(c => c.PublicationId == publicationId);

            // Act
            await publicationService.CreateCommentAsync(dto, publicationId);

            // Assert
            Assert.That(dbContext.Comments.Count(c => c.PublicationId == publicationId), Is.EqualTo(countBefore + 1));
        }

        [Test]
        public async Task CreateCommentAsync_ValidDto_CreatesCommentWithCorrectContent()
        {
            // Arrange
            var dto = new CommentFormDto()
            {
                Content = "This is a test comment"
            };
            var publicationId = dbContext.Publications.First().Id;

            // Act
            await publicationService.CreateCommentAsync(dto, publicationId);

            // Assert
            Assert.That(dbContext.Comments.Last().Content, Is.EqualTo(dto.Content));
        }

        [Test]
        public async Task CreateCommentAsync_InValidPublicationId_ThrowsNullReferenceException()
        {
            // Arrange
            var dto = new CommentFormDto()
            {
                Content = "This is a test comment"
            };
            var invalidPublicationId = Guid.NewGuid(); // Use a non-existing ID

            // Act and Assert
            var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await publicationService.CreateCommentAsync(dto, invalidPublicationId);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(PublicationNotFound));
        }

        [Test]
        public async Task UpdateCommentAsync_ValidDtoAndId_UpdatesComment()
        {
            // Arrange
            var dto = new CommentFormDto()
            {
                Content = "This is a test comment"
            };
            var comment =
                dbContext.Comments.First(c => c.AuthorId.ToString() == "123400ce-d726-4fc8-83d9-d6b3ac1f591e");

            // Act
            await publicationService.UpdateCommentAsync(dto, comment.Id);

            // Assert
            Assert.That(comment.Content, Is.EqualTo(dto.Content));
        }

        [Test]
        public async Task UpdateCommentAsync_ThrowsNullReferenceException()
        {
            // Arrange
            var dto = new CommentFormDto()
            {
                Content = "This is a test comment"
            };
            var invalidCommentId = Guid.NewGuid(); // Use a non-existing ID

            // Act and Assert
            var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await publicationService.UpdateCommentAsync(dto, invalidCommentId);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(CommentNotFound));
        }

        [Test]
        public async Task UpdateCommentAsync_ThrowsAccessViolationExceptionException()
        {
            // Arrange
            var dto = new CommentFormDto()
            {
                Content = "This is a test comment"
            };
            var comment =
                dbContext.Comments.First(c => c.AuthorId.ToString() == "123456ed-2e82-4f5a-a684-a9c7e3ccb52e");

            // Act and Assert
            var exception = Assert.ThrowsAsync<AccessViolationException>(async () =>
            {
                await publicationService.UpdateCommentAsync(dto, comment.Id);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(NotAuthorizedToEditComment));
        }

        [Test]
        public async Task DeleteCommentAsync_ValidId_DeletesComment()
        {
            // Arrange
            var comment =
                dbContext.Comments.First(c => c.AuthorId.ToString() == "123400ce-d726-4fc8-83d9-d6b3ac1f591e");
            var countBefore = dbContext.Comments.Count();

            // Act
            await publicationService.DeleteCommentAsync(comment.Id);

            // Assert
            Assert.That(dbContext.Comments.Count(), Is.EqualTo(countBefore - 1));
        }

        [Test]
        public async Task DeleteCommentAsync_ThrowsNullReferenceException()
        {
            // Arrange
            var invalidCommentId = Guid.NewGuid(); // Use a non-existing ID

            // Act and Assert
            var exception = Assert.ThrowsAsync<NullReferenceException>(async () =>
            {
                await publicationService.DeleteCommentAsync(invalidCommentId);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(CommentNotFound));
        }

        [Test]
        public async Task DeleteCommentAsync_ThrowsAccessViolationExceptionException()
        {
            // Arrange
            var comment =
                dbContext.Comments.First(c => c.AuthorId.ToString() == "123456ed-2e82-4f5a-a684-a9c7e3ccb52e");

            // Act and Assert
            var exception = Assert.ThrowsAsync<AccessViolationException>(async () =>
            {
                await publicationService.DeleteCommentAsync(comment.Id);
            });

            // Optionally assert on the exception message or other details
            Assert.That(exception!.Message, Is.EqualTo(NotAuthorizedToDeleteComment));
        }

        [Test]
        public async Task GetCommentsOnPublicationAsync_ValidPublicationId_ReturnsComments()
        {
            // Arrange
            var publicationId = Guid.Parse("a0a0a6a0-0b1e-4b9e-9b0a-0b9b9b9b9b9b");

            // Act
            var result = await publicationService.GetCommentsOnPublicationAsync(publicationId);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<IEnumerable<CommentDto>>(result);

            // Assuming some comments were seeded during setup
            Assert.IsTrue(result.Any());
        }
    }
}
