import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import type { Book, BookInput, ValidationErrors } from '../types'

interface Props {
  /** Книга для редактирования; null — режим добавления. */
  book: Book | null
  saving: boolean
  errors: ValidationErrors
  onSubmit: (input: BookInput) => void
  onCancel: () => void
}

interface FormState {
  title: string
  author: string
  genre: string
  year: string
  rating: string
  description: string
  isRead: boolean,
  pages: string
}

const EMPTY: FormState = {
  title: '',
  author: '',
  genre: '',
  year: '',
  rating: '',
  description: '',
  isRead: false,
  pages: ''
}

function toFormState(book: Book | null): FormState {
  if (!book) return EMPTY
  return {
    title: book.title,
    author: book.author,
    genre: book.genre ?? '',
    year: book.year?.toString() ?? '',
    rating: book.rating?.toString() ?? '',
    description: book.description ?? '',
    isRead: book.isRead,
    pages: book.pages?.toString() ?? '',
  }
}

export default function BookForm({ book, saving, errors, onSubmit, onCancel }: Props) {
  const [form, setForm] = useState<FormState>(() => toFormState(book))

  // При выборе другой книги подставляем её данные в поля.
  useEffect(() => {
    setForm(toFormState(book))
  }, [book])

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    onSubmit({
      title: form.title.trim(),
      author: form.author.trim(),
      genre: form.genre.trim() || null,
      year: form.year ? Number(form.year) : null,
      rating: form.rating ? Number(form.rating) : null,
      description: form.description.trim() || null,
      isRead: form.isRead,
      pages:  form.pages ? Number(form.pages) : null,
    })
  }

  const fieldError = (name: string) => errors[name]?.[0]

  return (
    <form className="card form" onSubmit={handleSubmit}>
      <h2>{book ? 'Редактировать книгу' : 'Добавить книгу'}</h2>

      <label>
        Название
        <input
          name="title"
          value={form.title}
          onChange={(e) => update('title', e.target.value)}
          placeholder="Мастер и Маргарита"
        />
        {fieldError('Title') && <span className="field-error">{fieldError('Title')}</span>}
      </label>

      <label>
        Автор
        <input
          name="author"
          value={form.author}
          onChange={(e) => update('author', e.target.value)}
          placeholder="Михаил Булгаков"
        />
        {fieldError('Author') && <span className="field-error">{fieldError('Author')}</span>}
      </label>

      <div className="form-row">
        <label>
          Жанр
          <input
            name="genre"
            value={form.genre}
            onChange={(e) => update('genre', e.target.value)}
            placeholder="Роман"
          />
          {fieldError('Genre') && <span className="field-error">{fieldError('Genre')}</span>}
        </label>

        <label>
          Год
          <input
            type="number"
            name="year"
            value={form.year}
            onChange={(e) => update('year', e.target.value)}
            placeholder="1967"
          />
          {fieldError('Year') && <span className="field-error">{fieldError('Year')}</span>}
        </label>

        <label>
          Оценка
          <select
            name="rating"
            value={form.rating}
            onChange={(e) => update('rating', e.target.value)}
          >
            <option value="">—</option>
            {[1, 2, 3, 4, 5].map((n) => (
              <option key={n} value={n}>
                {n}
              </option>
            ))}
          </select>
          {fieldError('Rating') && <span className="field-error">{fieldError('Rating')}</span>}
        </label>
      </div>
      <div>
         <label>
          Количество страниц в книге
          <input
            type="number"
            name="pages"
            value={form.pages}
            onChange={(e) => update('pages', e.target.value)}
            placeholder="100"
          />
          {fieldError('Pages') && <span className="field-error">{fieldError('Pages')}</span>}
        </label>

      </div>

      <label>
        Описание
        <textarea
          rows={3}
          name="description"
          value={form.description}
          onChange={(e) => update('description', e.target.value)}
          placeholder="Пара слов о книге"
        />
        {fieldError('Description') && (
          <span className="field-error">{fieldError('Description')}</span>
        )}
      </label>

      <label className="checkbox">
        <input
          type="checkbox"
          name="isRead"
          checked={form.isRead}
          onChange={(e) => update('isRead', e.target.checked)}
        />
        Прочитана
      </label>

      <div className="form-actions">
        <button type="submit" className="primary" disabled={saving}>
          {saving ? 'Сохранение…' : book ? 'Сохранить' : 'Добавить'}
        </button>
        {book && (
          <button type="button" onClick={onCancel} disabled={saving}>
            Отмена
          </button>
        )}
      </div>
    </form>
  )
}
