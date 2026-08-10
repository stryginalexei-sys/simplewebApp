import { useCallback, useEffect, useState } from 'react'
import BookForm from './components/BookForm'
import BookList from './components/BookList'
import Toolbar from './components/Toolbar'
import { ApiError, createBook, deleteBook, getBooks, getGenres, updateBook } from './api'
import { DEFAULT_SORT } from './types'
import type { Book, BookInput, SortOption, ValidationErrors } from './types'

export default function App() {
  const [books, setBooks] = useState<Book[]>([])
  const [genres, setGenres] = useState<string[]>([])
  const [search, setSearch] = useState('')
  const [genre, setGenre] = useState('')
  const [sort, setSort] = useState<SortOption>(DEFAULT_SORT)

  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [editing, setEditing] = useState<Book | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [fieldErrors, setFieldErrors] = useState<ValidationErrors>({})

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [nextBooks, nextGenres] = await Promise.all([
        getBooks(search, genre, sort),
        getGenres(),
      ])
      setBooks(nextBooks)
      setGenres(nextGenres)
      setError(null)
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Не удалось загрузить каталог')
    } finally {
      setLoading(false)
    }
  }, [search, genre, sort])

  // Перезагружаем список при смене поиска/фильтра, с небольшой задержкой на ввод.
  useEffect(() => {
    const timer = setTimeout(load, 250)
    return () => clearTimeout(timer)
  }, [load])

  async function handleSubmit(input: BookInput) {
    setSaving(true)
    setFieldErrors({})
    try {
      if (editing) {
        await updateBook(editing.id, input)
        setEditing(null)
      } else {
        await createBook(input)
      }
      setError(null)
      await load()
    } catch (e) {
      if (e instanceof ApiError) {
        setError(e.message)
        setFieldErrors(e.errors)
      } else {
        setError('Не удалось сохранить книгу')
      }
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(book: Book) {
    if (!confirm(`Удалить «${book.title}»?`)) return

    try {
      await deleteBook(book.id)
      if (editing?.id === book.id) setEditing(null)
      setError(null)
      await load()
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'Не удалось удалить книгу')
    }
  }

  function handleEdit(book: Book) {
    setEditing(book)
    setFieldErrors({})
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  function handleCancel() {
    setEditing(null)
    setFieldErrors({})
  }

  return (
    <div className="page">
      <header className="page-header">
        <h1>Каталог книг</h1>
        <p className="subtitle">
          {loading ? 'Загрузка…' : `Книг в каталоге: ${books.length}`}
        </p>
      </header>

      {error && <div className="banner error">{error}</div>}

      <BookForm
        book={editing}
        saving={saving}
        errors={fieldErrors}
        onSubmit={handleSubmit}
        onCancel={handleCancel}
      />

      <Toolbar
        search={search}
        genre={genre}
        sort={sort}
        genres={genres}
        onSearchChange={setSearch}
        onGenreChange={setGenre}
        onSortChange={setSort}
      />

      {!loading && books.length === 0 ? (
        <p className="empty">
          {search || genre ? 'Ничего не найдено.' : 'Каталог пуст — добавьте первую книгу.'}
        </p>
      ) : (
        <BookList
          books={books}
          editingId={editing?.id ?? null}
          onEdit={handleEdit}
          onDelete={handleDelete}
        />
      )}
    </div>
  )
}
