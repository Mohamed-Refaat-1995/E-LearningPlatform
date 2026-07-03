namespace ELearningPlatform.Core.Interfaces;

public class SeedDemoDataRequest
{
    public string VideosFolderPath { get; set; } =
        @"D:\Computer Science Diploma Cairo university\Second year\2 nd Term\CS 599 Final Dioloma Project\for test\course 2\videos";

    public string ResourcesFolderPath { get; set; } =
        @"D:\Computer Science Diploma Cairo university\Second year\2 nd Term\CS 599 Final Dioloma Project\for test\course 2\Resources";

    public string QuestionsDocPath { get; set; } =
        @"D:\Computer Science Diploma Cairo university\Second year\2 nd Term\CS 599 Final Dioloma Project\for test\course 2\Questions\Programming_1000_Questions.docx";

    /// <summary>How many of the smallest video files in the folder to actually upload to Cloudinary (URLs are cycled/reused across lessons).</summary>
    public int MaxVideosToUpload { get; set; } = 12;

    public int InstructorCount { get; set; } = 10;
    public int CoursesPerInstructor { get; set; } = 3;
}

public class SeedDemoDataResult
{
    public int InstructorsCreated { get; set; }
    public int InstructorsSkipped { get; set; }
    public int CoursesCreated { get; set; }
    public int SectionsCreated { get; set; }
    public int LessonsCreated { get; set; }
    public int QuizzesCreated { get; set; }
    public int VideosUploaded { get; set; }
    public int ResourcesUploaded { get; set; }
}

public interface IDemoDataSeederService
{
    Task<SeedDemoDataResult> SeedAsync(SeedDemoDataRequest request);
}
