import type { Book, BookInput, ValidationErrors } from './types'

const BASE = '/api'

/** Ошибка запроса: message для показа, errors — ошибки валидации по полям. */
export class ApiError extends Error {
  errors: ValidationErrors

  constructor(message: string, errors: ValidationErrors = {}) {
    super(message)
    this.name = 'ApiError'
    this.errors = errors
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...init,
    })
  } catch {
    throw new ApiError('Не удалось связаться с сервером. Он запущен?')
  }

  if (!response.ok) {
    // ASP.NET Core отдаёт ошибки валидации как ProblemDetails с полем errors.
    const problem = await response.json().catch(() => null)
    const errors: ValidationErrors = problem?.errors ?? {}
    const message =
      Object.values(errors).flat()[0] ??
      problem?.title ??
      `Ошибка ${response.status}`
    throw new ApiError(message, errors)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

export function getBooks(search: string, genre: string): Promise<Book[]> {
  const params = new URLSearchParams()
  if (search.trim()) params.set('search', search.trim())
  if (genre) params.set('genre', genre)

  const query = params.toString()
  return request<Book[]>(`/books${query ? `?${query}` : ''}`)
}

export function getGenres(): Promise<string[]> {
  return request<string[]>('/genres')
}

export function createBook(input: BookInput): Promise<Book> {
  return request<Book>('/books', { method: 'POST', body: JSON.stringify(input) })
}

export function updateBook(id: number, input: BookInput): Promise<Book> {
  return request<Book>(`/books/${id}`, { method: 'PUT', body: JSON.stringify(input) })
}

export function deleteBook(id: number): Promise<void> {
  return request<void>(`/books/${id}`, { method: 'DELETE' })
}
