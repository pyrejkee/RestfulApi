﻿using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Library.API.Entities;
using Library.API.Helpers;
using Library.API.Models;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Library.API.Controllers
{
    [Route("api/authors")]
    public class AuthorsController : Controller
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUrlHelper _urlHelper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly ITypeHelperService _typeHelperService;

        public AuthorsController(ILibraryRepository libraryRepository,
            IUrlHelper urlHelper,
            IPropertyMappingService propertyMappingService,
            ITypeHelperService typeHelperService)
        {
            _libraryRepository = libraryRepository;
            _urlHelper = urlHelper;
            _propertyMappingService = propertyMappingService;
            _typeHelperService = typeHelperService;
        }

        [HttpGet(Name = "GetAuthors")]
        public IActionResult GetAuthors(AuthorResourceParameters authorResourceParameters,
            [FromHeader(Name = "Accept")] string mediaType)
        {
            if(!_propertyMappingService.ValidMappingExistsFor<AuthorDto, Author>
                (authorResourceParameters.OrderBy))
            {
                return BadRequest();
            }

            if(!_typeHelperService.TypeHasProperties<AuthorDto>(authorResourceParameters.Fields))
            {
                return BadRequest();
            }

            var authorsFromRepo = _libraryRepository.GetAuthors(authorResourceParameters);

            var authors = Mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo);

            if (mediaType == "application/vnd.marvin.hateoas+json")
            {
                var paginationMetadata = new
                {
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };
                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                var links = CreateLinksForAuthors(authorResourceParameters, authorsFromRepo.HasNext,
                    authorsFromRepo.HasPrevious);
                var shapedAuthors = authors.ShapeData(authorResourceParameters.Fields);
                var shapedAuthorsWithLinks = shapedAuthors.Select(author =>
                {
                    var authorAsDictionary = author as IDictionary<string, object>;
                    var authorLinks = CreateLinksForAuthor(
                        (Guid)authorAsDictionary["Id"], authorResourceParameters.Fields);
                    authorAsDictionary.Add("links", authorLinks);

                    return authorAsDictionary;
                });
                var linkedCollectionResource = new
                {
                    value = shapedAuthorsWithLinks,
                    links = links
                };


                return Ok(linkedCollectionResource);
            }
            else
            {
                var previousPageLink = authorsFromRepo.HasPrevious
                    ? CreateAuthorResourceUri(authorResourceParameters, ResourceUriType.PreviousPage)
                    : null;
                var nextPageLink = authorsFromRepo.HasNext
                    ? CreateAuthorResourceUri(authorResourceParameters, ResourceUriType.NextPage)
                    : null;
                var paginationMetadata = new
                {
                    previousPageLink = previousPageLink,
                    nextPageLink = nextPageLink,
                    totalCount = authorsFromRepo.TotalCount,
                    pageSize = authorsFromRepo.PageSize,
                    currentPage = authorsFromRepo.CurrentPage,
                    totalPages = authorsFromRepo.TotalPages
                };
                Response.Headers.Add("X-Pagination", Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

                return Ok(authors.ShapeData(authorResourceParameters.Fields));
            }
        }

        private string CreateAuthorResourceUri(AuthorResourceParameters authorResourceParameters,
            ResourceUriType type)
        {
            switch (type)
            {
                case ResourceUriType.PreviousPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorResourceParameters.Fields,
                            orderBy = authorResourceParameters.OrderBy,
                            searchQuery = authorResourceParameters.SearchQuery,
                            genre = authorResourceParameters.Genre,
                            pageNumber = authorResourceParameters.PageNumber - 1,
                            pageSize = authorResourceParameters.PageSize
                        });
                case ResourceUriType.NextPage:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorResourceParameters.Fields,
                            orderBy = authorResourceParameters.OrderBy,
                            searchQuery = authorResourceParameters.SearchQuery,
                            genre = authorResourceParameters.Genre,
                            pageNumber = authorResourceParameters.PageNumber + 1,
                            pageSize = authorResourceParameters.PageSize
                        });
                case ResourceUriType.Current:
                default:
                    return _urlHelper.Link("GetAuthors",
                        new
                        {
                            fields = authorResourceParameters.Fields,
                            orderBy = authorResourceParameters.OrderBy,
                            searchQuery = authorResourceParameters.SearchQuery,
                            genre = authorResourceParameters.Genre,
                            pageNumber = authorResourceParameters.PageNumber,
                            pageSize = authorResourceParameters.PageSize
                        });
            }
        }

        [HttpGet("{id}", Name = "GetAuthor")]
        public IActionResult GetAuthor(Guid id, [FromQuery] string fields)
        {
            if(!_typeHelperService.TypeHasProperties<AuthorDto>(fields))
            {
                return BadRequest();
            }

            var authorFromRepo = _libraryRepository.GetAuthor(id);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var author = Mapper.Map<AuthorDto>(authorFromRepo);

            var links = CreateLinksForAuthor(id, fields);
            var linkedResourceToReturn = author.ShapeData(fields)
                as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type", new [] { "application/vnd.marvin.author.full+json" })]
        public IActionResult CreateAuthor([FromBody] AuthorForCreationDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(author);
            _libraryRepository.AddAuthor(authorEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinksForAuthor(authorToReturn.Id, null);
            var linkedResourceToReturn = authorToReturn.ShapeData(null)
                as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new {Id = linkedResourceToReturn["Id"]}, linkedResourceToReturn);
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type", new[]
        {
            "application/vnd.marvin.authorwithdateofdeath.full+json",
            "application/vnd.marvin.authorwithdateofdeath.full+xml"
        })]
        public IActionResult CreateAuthorWithDateOfDeath([FromBody] AuthorForCreationWithDateOfDeathDto author)
        {
            if (author == null)
            {
                return BadRequest();
            }

            var authorEntity = Mapper.Map<Author>(author);
            _libraryRepository.AddAuthor(authorEntity);
            if (!_libraryRepository.Save())
            {
                throw new Exception("Creating an author failed on save.");
            }

            var authorToReturn = Mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinksForAuthor(authorToReturn.Id, null);
            var linkedResourceToReturn = authorToReturn.ShapeData(null)
                as IDictionary<string, object>;
            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor", new { Id = linkedResourceToReturn["Id"] }, linkedResourceToReturn);
        }

        [HttpPost("{id}")]
        public IActionResult BlockAuthorCreation(Guid id)
        {
            if(_libraryRepository.AuthorExists(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            return NotFound();
        }

        [HttpDelete("{id}", Name = "DeleteAuthor")]
        public IActionResult DeleteAuthor(Guid id)
        {
            var authorFromRepo = _libraryRepository.GetAuthor(id);
            if(authorFromRepo == null)
            {
                return NotFound();
            }

            _libraryRepository.DeleteAuthor(authorFromRepo);
            if(!_libraryRepository.Save())
            {
                throw new Exception($"Deleting author {id} failed on save.");
            }

            return NoContent();
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid id, string fields)
        {
            var links = new List<LinkDto>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                links.Add(new LinkDto(
                    _urlHelper.Link("GetAuthor", new { id = id }),
                    "self",
                    "GET"));
            }
            else
            {
                links.Add(new LinkDto(
                    _urlHelper.Link("GetAuthor", new { id= id, fields = fields}),
                    "self",
                    "GET"));
            }

            links.Add(new LinkDto(
                _urlHelper.Link("GetAuthor", new { id = id }),
                "delete_author",
                "DELETE"));

            // As the author exists - we can now 
            // 1) create a book for author
            // 2) get all book for author
            links.Add(new LinkDto(
                _urlHelper.Link("CreateBookForAuthor", new { authorId = id }),
                "create_book_for_author",
                "POST"));
            links.Add(new LinkDto(
                _urlHelper.Link("GetBooksForAuthor", new { authorId = id }),
                "books",
                "GET"));

            return links;
        }

        private IEnumerable<LinkDto> CreateLinksForAuthors(
            AuthorResourceParameters authorResourceParameters, bool hasNext, bool hasPrevious)
        {
            var links = new List<LinkDto>();

            // self
            links.Add(new LinkDto(
                CreateAuthorResourceUri(authorResourceParameters, ResourceUriType.Current),
                "self",
                "GET"));
            if (hasNext)
            {
                links.Add(new LinkDto(
                        CreateAuthorResourceUri(authorResourceParameters, ResourceUriType.NextPage),
                        "nextPage",
                        "GET"));
            }

            if (hasPrevious)
            {
                links.Add(new LinkDto(
                    CreateAuthorResourceUri(authorResourceParameters, ResourceUriType.PreviousPage),
                    "previousPage",
                    "GET"));
            }


            return links;
        }
    }
}