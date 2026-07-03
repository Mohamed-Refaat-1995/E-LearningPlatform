using System.IO.Compression;
using System.Text.RegularExpressions;
using ELearningPlatform.Core;
using ELearningPlatform.Core.Interfaces;

namespace ELearningPlatform.Application.Services;

public class DemoDataSeederService : IDemoDataSeederService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryVideoService _cloudinaryVideoService;

    private static readonly (string First, string Last)[] InstructorNames =
    {
        ("Ahmed", "ElSayed"), ("Sara", "Fathy"), ("Omar", "Hassan"), ("Mona", "Ibrahim"),
        ("Youssef", "Mostafa"), ("Nourhan", "Abdelrahman"), ("Karim", "Saeed"), ("Heba", "Nabil"),
        ("Tarek", "Ashraf"), ("Aya", "Ramzy")
    };

    private static readonly Dictionary<int, string[]> CategoryTopics = new()
    {
        [1] = new[] { "Modern JavaScript & ES2024", "Full-Stack .NET & Angular", "React from Zero to Hero", "Advanced CSS & Tailwind", "Node.js API Development" },
        [2] = new[] { "Flutter App Development", "Native Android with Kotlin", "iOS Development with Swift", "React Native in Practice", "Cross-Platform Mobile Apps" },
        [3] = new[] { "Python for Data Science", "Machine Learning Foundations", "Data Analysis with Pandas", "SQL for Data Analysts", "Deep Learning Basics" },
        [4] = new[] { "UI/UX Design Principles", "Figma for Product Designers", "Design Systems in Practice", "Motion Design Fundamentals", "Branding & Visual Identity" },
        [5] = new[] { "Digital Marketing Essentials", "Project Management Fundamentals", "Freelancing & Entrepreneurship", "Business Analytics", "Leadership & Communication Skills" },
    };

    private static readonly string[] LessonNameTemplates =
    {
        "Introduction & Course Overview", "Setting Up Your Environment", "Core Concepts Explained",
        "Hands-On Walkthrough", "Building Your First Project", "Common Pitfalls & Best Practices",
        "Intermediate Techniques", "Real-World Case Study", "Putting It All Together", "Wrap-Up & Next Steps"
    };

    private static readonly decimal[] PriceOptions = { 19.99m, 29.99m, 39.99m, 49.99m, 59.99m, 69.99m };

    public DemoDataSeederService(IUnitOfWork unitOfWork, ICloudinaryVideoService cloudinaryVideoService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryVideoService = cloudinaryVideoService;
    }

    public async Task<SeedDemoDataResult> SeedAsync(SeedDemoDataRequest request)
    {
        var result = new SeedDemoDataResult();

        var categories = (await _unitOfWork.Categories.GetAllAsync()).Where(c => !c.IsDeleted).ToList();
        if (categories.Count == 0)
            throw new InvalidOperationException("No categories found. Seed categories before running the demo data seeder.");

        var uploadedVideos = await UploadFolderAsync(
            request.VideosFolderPath, request.MaxVideosToUpload, isVideo: true);
        result.VideosUploaded = uploadedVideos.Count;
        if (uploadedVideos.Count == 0)
            throw new InvalidOperationException("No video files were uploaded — check VideosFolderPath.");

        var uploadedResources = await UploadFolderAsync(
            request.ResourcesFolderPath, int.MaxValue, isVideo: false);
        result.ResourcesUploaded = uploadedResources.Count;

        var questions = ParseQuestionsDocx(request.QuestionsDocPath);

        var instructorCount = Math.Min(request.InstructorCount, InstructorNames.Length);
        var globalCourseIndex = 0;
        var globalLessonIndex = 0;
        var resourceCursor = 0;

        for (var i = 0; i < instructorCount; i++)
        {
            var email = $"instructor{i + 1}@elearning.com";
            var existing = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existing != null)
            {
                result.InstructorsSkipped++;
                continue;
            }

            var (firstName, lastName) = InstructorNames[i];
            var instructor = new Instructor
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("@Mohamed1995@"),
                Role = UserRoleEnum.Instructor,
                IsEmailVerified = true,
                IsActive = true,
                Bio = $"{firstName} {lastName} is a seasoned instructor sharing practical, project-based courses."
            };

            var primaryCategoryId = categories[i % categories.Count].Id;
            var secondaryCategoryId = categories[(i + 2) % categories.Count].Id;

            for (var c = 0; c < request.CoursesPerInstructor; c++)
            {
                var categoryId = c == 1 ? secondaryCategoryId : primaryCategoryId;
                var topics = CategoryTopics.TryGetValue(categoryId, out var t) ? t : CategoryTopics[1];
                var title = $"{topics[(i + c) % topics.Length]} — with {firstName} {lastName}";

                var isFreeCourse = (i + c) % 3 == 0;
                var hasResources = globalCourseIndex % 2 == 0;
                var hasQuiz = globalCourseIndex % 3 != 0;

                var course = new Course
                {
                    Title = title,
                    Description = $"A hands-on {topics[(i + c) % topics.Length]} course covering everything you need to go from fundamentals to real-world application.",
                    Price = isFreeCourse ? 0m : PriceOptions[globalCourseIndex % PriceOptions.Length],
                    CategoryId = categoryId,
                    Level = (CourseLevelEnum)((c % 3) + 1),
                    Instructor = instructor,
                    IsPublished = true,
                    PublishedAt = DateTime.UtcNow.AddDays(-globalCourseIndex)
                };

                var sectionCount = c % 2 == 0 ? 4 : 3;
                Lesson? lastLesson = null;

                for (var s = 0; s < sectionCount; s++)
                {
                    var section = new Section
                    {
                        Title = $"Section {s + 1}: {topics[(i + c + s) % topics.Length]}",
                        DisplayOrder = s + 1,
                        Course = course
                    };

                    var lessonCount = 3 + (s % 3);
                    for (var l = 0; l < lessonCount; l++)
                    {
                        var video = uploadedVideos[globalLessonIndex % uploadedVideos.Count];
                        var lesson = new Lesson
                        {
                            Title = LessonNameTemplates[(s * 5 + l) % LessonNameTemplates.Length],
                            DisplayOrder = l + 1,
                            DurationMinutes = video.DurationSeconds.HasValue
                                ? Math.Max(1, (int)Math.Ceiling(video.DurationSeconds.Value / 60.0))
                                : 5 + (globalLessonIndex % 20),
                            IsPreview = isFreeCourse || (s == 0 && l == 0),
                            ContentType = "Video",
                            VideoUrl = video.SecureUrl,
                            VideoPublicId = video.PublicId,
                            Section = section
                        };

                        if (hasResources && uploadedResources.Count > 0 && l == 0)
                        {
                            var resource = uploadedResources[resourceCursor % uploadedResources.Count];
                            resourceCursor++;
                            lesson.ResourceUrl = resource.SecureUrl;
                            lesson.ResourcePublicId = resource.PublicId;
                        }

                        section.Lessons.Add(lesson);
                        lastLesson = lesson;
                        globalLessonIndex++;
                        result.LessonsCreated++;
                    }

                    course.Sections.Add(section);
                    result.SectionsCreated++;
                }

                if (hasQuiz && lastLesson != null && questions.Count > 0)
                {
                    var quiz = BuildQuiz(lastLesson, questions, globalCourseIndex);
                    lastLesson.Quiz = quiz;
                    result.QuizzesCreated++;
                }

                await _unitOfWork.Courses.AddAsync(course);

                globalCourseIndex++;
                result.CoursesCreated++;
            }

            await _unitOfWork.Users.AddAsync(instructor);
            await _unitOfWork.SaveChangesAsync();
            result.InstructorsCreated++;
        }

        return result;
    }

    private Quiz BuildQuiz(Lesson lesson, List<ParsedQuestion> questions, int courseIndex)
    {
        var quiz = new Quiz
        {
            Title = $"{lesson.Title} — Knowledge Check",
            Description = "Quick check to confirm you've understood the key concepts from this course.",
            TimeLimit = 10,
            PassingScore = 60,
            IsPublished = true,
            DisplayOrder = 1,
            Lesson = lesson
        };

        const int questionsPerQuiz = 6;
        for (var q = 0; q < questionsPerQuiz; q++)
        {
            var parsed = questions[(courseIndex * questionsPerQuiz + q) % questions.Count];
            var question = new Question
            {
                QuestionText = parsed.Text,
                QuestionType = parsed.Type,
                Points = 1,
                DisplayOrder = q + 1,
                Quiz = quiz
            };

            Answer? correctAnswer = null;
            for (var a = 0; a < parsed.Options.Count; a++)
            {
                var answer = new Answer
                {
                    AnswerText = parsed.Options[a],
                    IsCorrect = a == parsed.CorrectIndex,
                    DisplayOrder = a + 1,
                    Question = question
                };
                question.Answers.Add(answer);
                if (answer.IsCorrect) correctAnswer = answer;
            }

            question.CorrectAnswer = correctAnswer;
            quiz.Questions.Add(question);
        }

        return quiz;
    }

    private async Task<List<CloudinaryUploadResult>> UploadFolderAsync(string folderPath, int maxFiles, bool isVideo)
    {
        var uploads = new List<CloudinaryUploadResult>();
        if (!Directory.Exists(folderPath)) return uploads;

        var files = new DirectoryInfo(folderPath).GetFiles()
            .OrderBy(f => f.Length)
            .Take(maxFiles)
            .ToList();

        foreach (var file in files)
        {
            await using var stream = file.OpenRead();
            var uploadResult = isVideo
                ? await _cloudinaryVideoService.UploadVideoAsync(stream, file.Name)
                : await _cloudinaryVideoService.UploadFileAsync(stream, file.Name);
            uploads.Add(uploadResult);
        }

        return uploads;
    }

    private static List<ParsedQuestion> ParseQuestionsDocx(string path)
    {
        var parsed = new List<ParsedQuestion>();
        if (!File.Exists(path)) return parsed;

        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml");
        if (entry == null) return parsed;

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var xml = reader.ReadToEnd();
        var text = Regex.Replace(xml, "<[^>]+>", string.Empty);

        var matches = Regex.Matches(text, @"Q\d+\.\s*(.*?)Answer:\s*(True|False|[A-D])(?=Q\d+\.|$)", RegexOptions.Singleline);
        foreach (Match m in matches)
        {
            var body = m.Groups[1].Value.Trim();
            var answer = m.Groups[2].Value.Trim();

            var optionMatch = Regex.Match(body,
                @"^(?<stem>.*?)A\)\s*(?<a>.*?)B\)\s*(?<b>.*?)C\)\s*(?<c>.*?)D\)\s*(?<d>.*)$",
                RegexOptions.Singleline);

            if (optionMatch.Success)
            {
                parsed.Add(new ParsedQuestion
                {
                    Type = QuestionTypeEnum.MultipleChoice,
                    Text = optionMatch.Groups["stem"].Value.Trim(),
                    Options = new List<string>
                    {
                        optionMatch.Groups["a"].Value.Trim(),
                        optionMatch.Groups["b"].Value.Trim(),
                        optionMatch.Groups["c"].Value.Trim(),
                        optionMatch.Groups["d"].Value.Trim()
                    },
                    CorrectIndex = answer[0] - 'A'
                });
            }
            else
            {
                var stem = Regex.Replace(body, "^True or False:\\s*", string.Empty).Trim();
                parsed.Add(new ParsedQuestion
                {
                    Type = QuestionTypeEnum.TrueFalse,
                    Text = stem,
                    Options = new List<string> { "True", "False" },
                    CorrectIndex = answer.Equals("True", StringComparison.OrdinalIgnoreCase) ? 0 : 1
                });
            }
        }

        return parsed;
    }

    private class ParsedQuestion
    {
        public string Text { get; set; } = string.Empty;
        public QuestionTypeEnum Type { get; set; }
        public List<string> Options { get; set; } = new();
        public int CorrectIndex { get; set; }
    }
}
