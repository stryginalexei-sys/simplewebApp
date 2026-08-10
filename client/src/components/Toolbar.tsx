import { SORT_OPTIONS } from '../types'
import type { SortOption } from '../types'

interface Props {
  search: string
  genre: string
  sort: SortOption
  genres: string[]
  onSearchChange: (value: string) => void
  onGenreChange: (value: string) => void
  onSortChange: (value: SortOption) => void
}

export default function Toolbar({
  search,
  genre,
  sort,
  genres,
  onSearchChange,
  onGenreChange,
  onSortChange,
}: Props) {
  return (
    <div className="toolbar">
      <input
        className="search"
        type="search"
        value={search}
        onChange={(e) => onSearchChange(e.target.value)}
        placeholder="Поиск по названию или автору"
      />

      <select value={genre} onChange={(e) => onGenreChange(e.target.value)}>
        <option value="">Все жанры</option>
        {genres.map((g) => (
          <option key={g} value={g}>
            {g}
          </option>
        ))}
      </select>

      <select
        value={sort}
        onChange={(e) => onSortChange(e.target.value as SortOption)}
        aria-label="Сортировка"
      >
        {SORT_OPTIONS.map(({ value, label }) => (
          <option key={value} value={value}>
            {label}
          </option>
        ))}
      </select>
    </div>
  )
}
