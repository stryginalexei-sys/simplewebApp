import type { Book } from '../types'

interface Props {
  books: Book[]
  editingId: number | null
  onEdit: (book: Book) => void
  onDelete: (book: Book) => void
}

function stars(rating: number | null) {
  if (!rating) return null
  return '★'.repeat(rating) + '☆'.repeat(5 - rating)
}

export default function BookList({ books, editingId, onEdit, onDelete }: Props) {
  return (
    <ul className="book-list">
      {books.map((book) => (
        <li key={book.id} className={`card book${book.id === editingId ? ' editing' : ''}`}>
          <div className="book-head">
            <h3>{book.title}</h3>
            {book.rating && (
              <span className="stars" title={`Оценка: ${book.rating} из 5`}>
                {stars(book.rating)}
              </span>
            )}
          </div>

          <p className="book-author">{book.author}</p>

          <div className="tags">
            {book.genre && <span className="tag">{book.genre}</span>}
            {book.year && <span className="tag">{book.year}</span>}
            <span className={`tag${book.isRead ? ' tag-read' : ''}`}>
              {book.isRead ? 'Прочитана' : 'В планах'}
            </span>
          </div>

          {book.description && <p className="book-description">{book.description}</p>}

          <div className="book-actions">
            <button type="button" onClick={() => onEdit(book)}>
              Изменить
            </button>
            <button type="button" className="danger" onClick={() => onDelete(book)}>
              Удалить
            </button>
          </div>
        </li>
      ))}
    </ul>
  )
}
