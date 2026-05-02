export interface Quiz {
  id: number;
  title: string;
  description?: string;
  courseId: number;
  timeLimit: number;
  passingScore: number;
  isPublished: boolean;
  displayOrder: number;
  questions?: Question[];
}

export interface Question {
  id: number;
  quizId: number;
  questionText: string;
  questionType: string;
  points: number;
  displayOrder: number;
  answers?: Answer[];
}

export interface Answer {
  id: number;
  questionId: number;
  answerText: string;
  isCorrect: boolean;
  displayOrder: number;
}

export interface QuizResult {
  id: number;
  quizId: number;
  studentId: number;
  score: number;
  maxScore: number;
  percentage: number;
  isPassed: boolean;
  timeSpentSeconds: number;
  takenAt: Date;
  studentAnswers?: StudentAnswer[];
}

export interface StudentAnswer {
  id: number;
  questionId: number;
  selectedAnswerId?: number;
  textAnswer?: string;
  isCorrect: boolean;
}

export interface SubmitQuizRequest {
  answers: { [key: number]: number | null };
}

export interface QuizResultResponse {
  result: QuizResult;
  correctAnswers: { [key: number]: number };
}
