﻿using Application.Common.SlugGeneratorService;
using Application.Contracts.Word.Commands.Create;

namespace Application.Features.Word.Commands.CreateWord
{
    public class CreateWordCommandHandler : IRequestHandler<CreateWordCommand, string>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IDateTime _dateTime;
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ISlugGeneratorService _slugService;

        public CreateWordCommandHandler(ISlugGeneratorService slugService, IApplicationDbContext dbContext,
            IMapper mapper, IDateTime dateTime, ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _dateTime = dateTime;
            _currentUserService = currentUserService;
            _slugService = slugService;
        }

        public async Task<string> Handle(CreateWordCommand request, CancellationToken cancellationToken)
        {
            WordCategory category = await _dbContext.Categories.FindAsync(request.CategoryId);
            _ = category ?? throw new NotFoundException(nameof(WordCategory), request.CategoryId);

            var Word = _mapper.Map<Words>(request);

            Word.Category = category;
            Word.Slug = GenerateSlug(Word);
            Word.LastModifiedBy = Word.CreatedBy = _currentUserService.GetUserId() ?? Guid.Empty;
            Word.LastModifiedAt = Word.CreatedAt = _dateTime.Now;


            await _dbContext.Words.AddAsync(Word, cancellationToken);

            WordTimelineEvent timelineEvent = new WordTimelineEvent
            {
                WordId = Word.Id,
                Words = Word,
                EventType = TimelineEventType.Created,
                Time = _dateTime.Now,
                Note = "Word created",
                CreateBy = _currentUserService.GetUserId() ?? Guid.Empty

            };
            await _dbContext.WordTimelineEvents.AddAsync(timelineEvent, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Word.Slug;
        }

        private string GenerateSlug(Words job) =>
            _slugService.GenerateSlug($"{job.Value}-{job.CreatedAt.Year}-{job.CreatedAt.Month}");
    }
}
