# Sincronización Backend-Frontend: Estados de Postulaciones

## 📋 Estados Sincronizados

El backend (.NET) y el frontend (React/TypeScript) comparten los **mismos 7 estados** de postulaciones:

### Definiciones

| Estado | Valor | Descripción |
|--------|-------|------------|
| **Submitted** | `submitted` | Enviada por docente; pendiente revisión TH |
| **ThApproved** | `th_approved` | Aprobada por TH; pendiente CP |
| **ThRejected** | `th_rejected` | Rechazada por TH (final) |
| **CpRejected** | `cp_rejected` | Rechazada por CP; apelable en 3 días |
| **Appealed** | `appealed` | Apelada; pendiente CA |
| **Approved** | `approved` | Promoción aprobada (final) |
| **Rejected** | `rejected` | Rechazada definitivamente (final) |

## 🔄 Sincronización Actual

### ✅ Backend (C# .NET)
- **Ubicación:** `src/PromocionBackend.Domain/Constants/ApplicationStatus.cs`
- **Tipo:** Enum fuertemente tipado
- **Métodos Helper:** `ApplicationStatusExtensions.cs`
  - `ToStringValue()` - Convierte enum a string para serialización
  - `IsFinal()` - Verifica si estado es final
  - `IsInProgress()` - Verifica si está en progreso
  - Y más...

### ✅ Frontend (React/TypeScript)
- **Ubicación:** `src/app/types/api.ts`
- **Tipo:** Union type
- **Definición:**
```typescript
export type ApplicationStatus =
  | 'submitted'
  | 'th_approved'
  | 'th_rejected'
  | 'cp_rejected'
  | 'appealed'
  | 'approved'
  | 'rejected';
```

## 🔗 API Contracts

Todos los endpoints devuelven estados como **strings** en el JSON:

```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "status": "submitted",
    ...
  }
}
```

### Endpoints que usan estados

| Endpoint | Método | Parámetro | Validación |
|----------|--------|-----------|-----------|
| `/applications` | GET | `?status={status}` | ✅ Centralizada |
| `/dashboard/cp/data` | GET | `?status={status}` | ✅ Centralizada |
| `/applications/{id}` | GET | - | ✅ Estados en respuesta |
| `/applications/{id}/review` | POST | - | ✅ Transición validada |
| `/applications/{id}/appeal` | POST | - | ✅ Transición validada |

## 🛡️ Validación

### Backend
- **Método:** `ApplicationStatusValidator.IsValidStatus(string?)`
- **Ubicación:** `src/PromocionBackend.Application/Common/ApplicationStatusValidator.cs`
- **Uso:** Controllers validan parámetros `status` antes de procesar

### Frontend
- **Método:** El tipo `ApplicationStatus` es validado por TypeScript en compilación
- **Runtime:** Componentes usan union type para type-safety

## 📝 Reglas de Transición

Ambos lados respetan la **misma máquina de estados**:

```
submitted 
  ├─ [TH aproba] → th_approved → [CP aproba] → approved (FINAL)
  └─ [TH rechaza] → th_rejected (FINAL)

th_approved 
  └─ [CP rechaza] → cp_rejected → [Docente apela] → appealed → [CA decide]
                                └─ [Expira 3 días] → rejected (FINAL)
```

- **Backend:** `ApplicationStateMachine.cs` valida transiciones
- **Frontend:** `useApplicationUpdates.ts` hook refleja cambios en tiempo real

## 🔄 Mantener Sincronización

### Cuando se Agregan Nuevos Estados:

1. **Backend:** Actualizar `ApplicationStatus.cs` enum
2. **Backend:** Actualizar `ApplicationStatusExtensions.cs`
3. **Backend:** Actualizar `ApplicationStateMachine.cs` si hay nuevas transiciones
4. **Frontend:** Actualizar `src/app/types/api.ts` union type
5. **Both:** Actualizar tests

### Testing

```bash
# Backend
cd promocionBackend
dotnet test  # Verificar transiciones en ApplicationStateMachineTests

# Frontend
cd promocionDocente
npm test  # Verificar que la aplicación renderiza correctamente con nuevos estados
```

## ✨ Ventajas de Sincronización

- ✅ **Type-safe en ambos lados:** Enum en C#, union type en TypeScript
- ✅ **Validación centralizada:** Backend rechaza estados inválidos
- ✅ **Documentación viva:** Este archivo + comentarios en código
- ✅ **Misma fuente de verdad:** Estados definidos una sola vez en cada lado
- ✅ **Fácil mantención:** Cambios en un lado quedan claros en el otro

## 📞 Referencias

- Backend enum: `PromocionBackend.Domain.Constants.ApplicationStatus`
- Backend validator: `PromocionBackend.Application.Common.ApplicationStatusValidator`
- Frontend types: `promocionDocente/src/app/types/api.ts`
- Máquina de estados: `PromocionBackend.Domain.Services.ApplicationStateMachine`
