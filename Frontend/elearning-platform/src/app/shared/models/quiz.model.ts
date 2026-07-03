export interface Quiz {
  id: number;
  title: string;
  description?: string;
  lessonId: number;
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
  isCorrect?: boolean;
  displayOrder: number;
}

export interface QuizResult {
  id: number;
  quizId: number;
  quizTitle?: string;
  passingScore?: number;
  studentId: number;
  score: number;
  maxScore: number;
  percentage: number;
  isPassed: boolean;
  timeSpentSeconds: number;
  takenAt: Date;
  studentAnswers?: StudentAnswerDetail[];
}

export interface StudentAnswerDetail {
  id: number;
  questionId: number;
  questionText?: string;
  selectedAnswerId?: number;
  selectedAnswerText?: string;
  textAnswer?: string;
  isCorrect: boolean;
  correctAnswerText?: string;
}

export interface SubmitAnswerItem {
  questionId: number;
  selectedAnswerId?: number | null;
  textAnswer?: string;
}

export interface SubmitQuizRequest {
  answers: SubmitAnswerItem[];
  timeSpentSeconds?: number;
}

export interface CreateQuizRequest {
  title: string;
  description?: string;
  lessonId: number;
  timeLimit: number;
  passingScore: number;
  displayOrder: number;
}

export interface UpdateQuizRequest {
  title: string;
  description?: string;
  timeLimit: number;
  passingScore: number;
  displayOrder: number;
}

export interface CreateQuestionRequest {
  questionText: string;
  questionType: string;
  points: number;
  displayOrder: number;
}

export interface CreateAnswerRequest {
  answerText: string;
  isCorrect: boolean;
  displayOrder: number;
}
