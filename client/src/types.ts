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
  pages: number | null
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
  pages: number | null
}

/** Ошибки валидации с сервера: { "Title": ["Название обязательно."] } */
export type ValidationErrors = Record<string, string[]>

/**
 * Значения параметра sort. Должны совпадать с BookService.SortOptions на сервере —
 * неизвестное значение он отклоняет с 400.
 */
export const SORT_OPTIONS = [
  { value: 'author', label: 'По автору' },
  { value: 'title', label: 'По названию' },
  { value: 'year', label: 'По году' },
  { value: 'rating', label: 'По оценке' },
] as const

export type SortOption = (typeof SORT_OPTIONS)[number]['value']

export const DEFAULT_SORT: SortOption = 'author'
