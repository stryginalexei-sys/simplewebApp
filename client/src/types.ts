export interface Book {
  id: number
  title: string
  author: string
  genre: string | null
  year: number | null
  rating: number | null
  description: string | null
  isRead: boolean
  createdAt: string
}

/** Поля формы — то, что отправляем на сервер (без id и createdAt). */
export interface BookInput {
  title: string
  author: string
  genre: string | null
  year: number | null
  rating: number | null
  description: string | null
  isRead: boolean
}

/** Ошибки валидации с сервера: { "Title": ["Название обязательно."] } */
export type ValidationErrors = Record<string, string[]>
