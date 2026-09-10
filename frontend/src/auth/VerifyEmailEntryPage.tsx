import { Link, Navigate } from 'react-router-dom'

interface VerifyEmailEntryPageProps {
  readonly email: string | null
}

export function VerifyEmailEntryPage({ email }: VerifyEmailEntryPageProps) {
  if (email === null) return <Navigate to="/register" replace />

  return (
    <>
      <p>Check your email to verify your account.</p>
      <p>
        We sent a verification link to <strong>{email}</strong>.
      </p>
      <p className="form-switch">
        <Link className="primary-link" to="/login">
          Go to sign in
        </Link>
      </p>
    </>
  )
}
