import React, { useState, useEffect, useCallback } from "react"
import { Plus, Edit, Eye, EyeOff, Power, Save, X } from "lucide-react"
import { 
  getActiveIdentifierConfigs, 
  createIdentifierConfig, 
  toggleIdentifierActive, 
  updateIdentifierConfig,
  ValidationRuleType, 
  type IdentifierConfigDto, 
  type CreateIdentifierConfigCommand, 
  type ValidationRuleDto 
} from "@/services/api/identifiers"
import { useAuth } from "@/auth/use-auth"

const VALIDATION_RULE_TYPES = [
  { value: ValidationRuleType.Required, label: "Required" },
  { value: ValidationRuleType.MinLength, label: "Min Length" },
  { value: ValidationRuleType.MaxLength, label: "Max Length" },
  { value: ValidationRuleType.Pattern, label: "Pattern" },
  { value: ValidationRuleType.Email, label: "Email" },
  { value: ValidationRuleType.Phone, label: "Phone" },
  { value: ValidationRuleType.Range, label: "Range" },
  { value: ValidationRuleType.Custom, label: "Custom" },
]

export default function IdentifiersPage() {
  const { auth } = useAuth()
  const [configs, setConfigs] = useState<IdentifierConfigDto[]>([])
  const [loading, setLoading] = useState(true)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [includeDeactivated, setIncludeDeactivated] = useState(false)

  const loadConfigs = useCallback(async () => {
    try {
      setLoading(true)
      const data = await getActiveIdentifierConfigs(includeDeactivated)
      setConfigs(data)
      setError(null)
    } catch (err) {
      setError("Failed to load configurations")
      console.error(err)
    } finally {
      setLoading(false)
    }
  }, [includeDeactivated])

  useEffect(() => {
    loadConfigs()
  }, [loadConfigs])

  const handleCreateConfig = async (data: CreateIdentifierConfigCommand) => {
    try {
      setError(null)
      await createIdentifierConfig(data, auth!.accessToken)
      setShowCreateModal(false)
      loadConfigs()
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "Failed to create configuration"
      setError(errorMessage)
      throw err
    }
  }

  return (
    <div className="min-h-screen bg-gray-900 text-gray-100 p-6">
      <div className="max-w-6xl mx-auto">
        <div className="flex justify-between items-center mb-6">
          <h1 className="text-2xl font-bold text-gray-100">Identifier Configurations</h1>
          <button
            onClick={() => setShowCreateModal(true)}
            className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            <Plus className="w-4 h-4" />
            Create Configuration
          </button>
        </div>

        <div className="flex items-center gap-4 mb-6">
          <label className="flex items-center gap-2 text-sm text-gray-300 cursor-pointer">
            <input
              type="checkbox"
              checked={includeDeactivated}
              onChange={(e) => setIncludeDeactivated(e.target.checked)}
              className="w-4 h-4 text-blue-600 bg-gray-700 border-gray-600 rounded focus:ring-blue-500 focus:ring-2"
            />
            Show deactivated configurations
          </label>
          {includeDeactivated && (
            <span className="text-xs text-gray-500">
              Showing {configs.filter(c => !c.isActive).length} deactivated configurations
            </span>
          )}
        </div>

        {error && (
          <div className="mb-4 p-4 bg-red-900 border border-red-700 rounded-lg text-red-200">
            {error}
          </div>
        )}

        {loading ? (
          <div className="text-center py-8">
            <div className="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
            <p className="mt-2 text-gray-400">Loading...</p>
          </div>
        ) : (
          <div className="grid gap-4">
            {configs.map((config) => (
              <IdentifierConfigCard 
                key={config.id} 
                config={config} 
                onUpdate={loadConfigs}
              />
            ))}
          </div>
        )}

        {showCreateModal && (
          <CreateConfigModal
            onClose={() => setShowCreateModal(false)}
            onSubmit={handleCreateConfig}
          />
        )}
      </div>
    </div>
  )
}

function IdentifierConfigCard({ config, onUpdate }: { config: IdentifierConfigDto; onUpdate: () => void }) {
  const { auth } = useAuth()
  const [expanded, setExpanded] = useState(false)
  const [showEditModal, setShowEditModal] = useState(false)
  const [isActive, setIsActive] = useState(config.isActive)
  const [isToggling, setIsToggling] = useState(false)

  const handleToggleActive = async () => {
    try {
      setIsToggling(true)
      const newActiveState = !isActive
      await toggleIdentifierActive(config.id, newActiveState, auth!.accessToken)
      setIsActive(newActiveState)
      onUpdate()
    } catch (error) {
      console.error("Failed to toggle active state:", error)
    } finally {
      setIsToggling(false)
    }
  }

  const handleUpdateSuccess = () => {
    setShowEditModal(false)
    onUpdate()
  }

  return (
    <>
      <div className={`bg-gray-800 rounded-lg shadow-sm border p-4 ${
        config.isActive 
          ? "border-gray-700" 
          : "border-gray-600 opacity-75"
      }`}>
        <div className="flex justify-between items-start">
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <h3 className={`text-lg font-semibold ${
                config.isActive ? "text-gray-100" : "text-gray-400"
              }`}>{config.name}</h3>
              {!config.isActive && (
                <span className="px-2 py-1 bg-gray-600 text-gray-300 text-xs rounded-full">
                  Deactivated
                </span>
              )}
            </div>
            <p className="text-gray-400 text-sm mt-1">{config.description}</p>
            <div className="flex items-center gap-3 mt-2">
              <span className="text-gray-500 text-xs">
                {config.rules.length} rule{config.rules.length !== 1 ? "s" : ""}
              </span>
            </div>
          </div>
          <div className="flex gap-2 items-center">
            <button
              onClick={handleToggleActive}
              disabled={isToggling}
              className={`p-2 rounded-lg transition-colors ${
                isActive 
                  ? "text-green-400 hover:text-green-300 hover:bg-green-900" 
                  : "text-gray-500 hover:text-gray-400 hover:bg-gray-700"
              } ${isToggling ? "opacity-50 cursor-not-allowed" : ""}`}
              title={isActive ? "Active - Click to deactivate" : "Inactive - Click to activate"}
            >
              <Power className="w-5 h-5" />
            </button>
            
            <button
              onClick={() => setExpanded(!expanded)}
              className="p-2 text-gray-400 hover:text-gray-200 hover:bg-gray-700 rounded-lg transition-colors"
            >
              {expanded ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
            </button>
            <button 
              onClick={() => setShowEditModal(true)}
              className="p-2 text-gray-400 hover:text-gray-200 hover:bg-gray-700 rounded-lg transition-colors"
            >
              <Edit className="w-4 h-4" />
            </button>
          </div>
        </div>

        {expanded && (
          <div className="mt-4 pt-4 border-t border-gray-700">
            <h4 className="font-medium text-gray-100 mb-3">Validation Rules:</h4>
            <div className="space-y-2">
              {config.rules.map((rule, index) => (
                <div key={index} className="flex items-center justify-between p-3 bg-gray-700 rounded-lg">
                  <div className="flex-1">
                    <div className="font-medium text-sm text-gray-100">
                      {VALIDATION_RULE_TYPES.find(t => t.value === rule.type)?.label || rule.type}
                    </div>
                    <div className="text-xs text-gray-400 mt-1">
                      {rule.errorMessage || "Default error message"}
                    </div>
                  </div>
                  <div className="text-xs text-gray-500">Order: {rule.order}</div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {showEditModal && (
        <EditConfigModal
          config={config}
          onClose={() => setShowEditModal(false)}
          onSubmit={handleUpdateSuccess}
        />
      )}
    </>
  )
}

function CreateConfigModal({ onClose, onSubmit }: { onClose: () => void; onSubmit: (data: CreateIdentifierConfigCommand) => Promise<void> }) {
  const [formData, setFormData] = useState({
    name: "",
    description: "",
  })
  const [rules, setRules] = useState<ValidationRuleDto[]>([
    {
      type: ValidationRuleType.Required,
      parameters: {},
      order: 0,
    }
  ])
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!formData.name || !formData.description) {
      setError("Please fill in all required fields")
      return
    }

    if (rules.length === 0) {
      setError("Please add at least one validation rule")
      return
    }

    try {
      setSubmitting(true)
      setError(null)
      await onSubmit({
        ...formData,
        rules
      })
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "Failed to create configuration"
      setError(errorMessage)
    } finally {
      setSubmitting(false)
    }
  }

  const addRule = () => {
    const newRule: ValidationRuleDto = {
      type: ValidationRuleType.Required,
      parameters: {},
      order: rules.length,
    }
    setRules([...rules, newRule])
  }

  const updateRule = (index: number, rule: ValidationRuleDto) => {
    const updatedRules = [...rules]
    updatedRules[index] = rule
    setRules(updatedRules)
  }

  const removeRule = (index: number) => {
    if (rules.length > 1) {
      const updatedRules = rules.filter((_, i) => i !== index)
      setRules(updatedRules.map((rule, i) => ({ ...rule, order: i })))
    }
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center p-4 z-50">
      <div className="bg-gray-800 rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto border border-gray-700">
        <div className="p-6">
          <h2 className="text-xl font-bold text-gray-100 mb-6">Create Identifier Configuration</h2>
          
          {error && (
            <div className="mb-4 p-3 bg-red-900 border border-red-700 rounded-lg text-red-200 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Name</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="e.g., Employee ID"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                rows={3}
                placeholder="Describe this identifier configuration"
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-3">
                <label className="block text-sm font-medium text-gray-300">Validation Rules</label>
                <button
                  type="button"
                  onClick={addRule}
                  className="px-3 py-1 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Add Rule
                </button>
              </div>
              <div className="space-y-3">
                {rules.map((rule, index) => (
                  <ValidationRuleEditor
                    key={index}
                    rule={rule}
                    onChange={(updatedRule) => updateRule(index, updatedRule)}
                    onRemove={() => removeRule(index)}
                    canRemove={rules.length > 1}
                  />
                ))}
              </div>
            </div>

            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={onClose}
                disabled={submitting}
                className="flex-1 px-4 py-2 bg-gray-700 text-gray-200 rounded-lg hover:bg-gray-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {submitting ? "Creating..." : "Create Configuration"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

function EditConfigModal({ config, onClose, onSubmit }: { 
  config: IdentifierConfigDto
  onClose: () => void
  onSubmit: () => void 
}) {
  const { auth } = useAuth()
  const [formData, setFormData] = useState({
    name: config.name,
    description: config.description,
  })
  const [rules, setRules] = useState<ValidationRuleDto[]>(config.rules)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [hasChanges, setHasChanges] = useState(false)

  useEffect(() => {
    const nameChanged = formData.name !== config.name
    const descChanged = formData.description !== config.description
    const rulesChanged = JSON.stringify(rules) !== JSON.stringify(config.rules)
    setHasChanges(nameChanged || descChanged || rulesChanged)
  }, [formData, rules, config])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    
    if (!formData.name || !formData.description) {
      setError("Please fill in all required fields")
      return
    }

    if (rules.length === 0) {
      setError("Please add at least one validation rule")
      return
    }

    try {
      setSubmitting(true)
      setError(null)
      await updateIdentifierConfig(config.id, {
        ...formData,
        rules
      }, auth!.accessToken)
      onSubmit()
    } catch (err: unknown) {
      const errorMessage = err instanceof Error ? err.message : "Failed to update configuration"
      setError(errorMessage)
    } finally {
      setSubmitting(false)
    }
  }

  const addRule = () => {
    const newRule: ValidationRuleDto = {
      type: ValidationRuleType.Required,
      parameters: {},
      order: rules.length,
    }
    setRules([...rules, newRule])
  }

  const updateRule = (index: number, rule: ValidationRuleDto) => {
    const updatedRules = [...rules]
    updatedRules[index] = rule
    setRules(updatedRules)
  }

  const removeRule = (index: number) => {
    if (rules.length > 1) {
      const updatedRules = rules.filter((_, i) => i !== index)
      setRules(updatedRules.map((rule, i) => ({ ...rule, order: i })))
    }
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-75 flex items-center justify-center p-4 z-50">
      <div className="bg-gray-800 rounded-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto border border-gray-700">
        <div className="p-6">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-xl font-bold text-gray-100">Edit Identifier Configuration</h2>
            {hasChanges && (
              <span className="px-3 py-1 bg-yellow-900 text-yellow-300 text-xs rounded-full">
                Unsaved Changes
              </span>
            )}
          </div>
          
          {error && (
            <div className="mb-4 p-3 bg-red-900 border border-red-700 rounded-lg text-red-200 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Name</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="e.g., Employee ID"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-300 mb-2">Description</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                rows={3}
                placeholder="Describe this identifier configuration"
              />
            </div>

            <div>
              <div className="flex justify-between items-center mb-3">
                <label className="block text-sm font-medium text-gray-300">Validation Rules</label>
                <button
                  type="button"
                  onClick={addRule}
                  className="px-3 py-1 bg-blue-600 text-white text-sm rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Add Rule
                </button>
              </div>
              <div className="space-y-3">
                {rules.map((rule, index) => (
                  <ValidationRuleEditor
                    key={index}
                    rule={rule}
                    onChange={(updatedRule) => updateRule(index, updatedRule)}
                    onRemove={() => removeRule(index)}
                    canRemove={rules.length > 1}
                  />
                ))}
              </div>
            </div>

            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={onClose}
                disabled={submitting}
                className="flex-1 px-4 py-2 bg-gray-700 text-gray-200 rounded-lg hover:bg-gray-600 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submitting || !hasChanges}
                className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
              >
                <Save className="w-4 h-4" />
                {submitting ? "Saving..." : "Save Changes"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}

function PatternTester({ pattern }: { pattern: string }) {
  const [testValue, setTestValue] = useState("")
  const [testResult, setTestResult] = useState<{
    isValid: boolean
    error?: string
  } | null>(null)

  const patternExamples = [
    { name: "Email", pattern: "^[\\w.-]+@[\\w.-]+\\.\\w+$", test: "test@example.com" },
    { name: "Phone", pattern: "^[\\d\\s\\-\\(\\)]+$", test: "123-456-7890" },
    { name: "Username", pattern: "^[a-zA-Z0-9_]{3,20}$", test: "user123" },
    { name: "Password", pattern: "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[a-zA-Z\\d@$!%*?&]{8,}$", test: "Password123" },
    { name: "URL", pattern: "^https?://[\\w\\.-]+\\.[a-zA-Z]{2,}(/.*)?$", test: "https://example.com" },
    { name: "Numbers only", pattern: "^\\d+$", test: "12345" },
    { name: "Letters only", pattern: "^[a-zA-Z]+$", test: "abc" },
    { name: "Alphanumeric", pattern: "^[a-zA-Z0-9]+$", test: "abc123" }
  ]

  const testPattern = () => {
    if (!pattern) {
      setTestResult({ isValid: false, error: "Pattern is required" })
      return
    }

    try {
      const regex = new RegExp(pattern)
      const isValid = regex.test(testValue)
      setTestResult({ isValid })
    } catch (error) {
      setTestResult({ 
        isValid: false, 
        error: error instanceof Error ? error.message : "Invalid regex pattern" 
      })
    }
  }

  const applyExample = (example: typeof patternExamples[0]) => {
    setTestValue(example.test)
  }

  return (
    <div className="mt-3 p-3 bg-gray-800 rounded-lg border border-gray-600">
      <div className="text-sm font-medium text-gray-300 mb-2">Pattern Tester</div>
      
      <div className="mb-3">
        <div className="text-xs text-gray-400 mb-1">Quick Examples:</div>
        <div className="flex flex-wrap gap-1">
          {patternExamples.map((example) => (
            <button
              key={example.name}
              type="button"
              onClick={() => applyExample(example)}
              className="px-2 py-1 bg-gray-700 text-gray-300 text-xs rounded hover:bg-gray-600 transition-colors"
              title={`Pattern: ${example.pattern}\nTest: ${example.test}`}
            >
              {example.name}
            </button>
          ))}
        </div>
      </div>

      <div className="space-y-2">
        <input
          type="text"
          value={testValue}
          onChange={(e) => setTestValue(e.target.value)}
          placeholder="Enter value to test pattern"
          className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
        <button
          type="button"
          onClick={testPattern}
          className="px-3 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700 transition-colors"
        >
          Test Pattern
        </button>
        
        {testResult && (
          <div className={`p-2 rounded text-xs ${
            testResult.isValid 
              ? "bg-green-900 text-green-300 border border-green-700" 
              : "bg-red-900 text-red-300 border border-red-700"
          }`}>
            {testResult.isValid ? "✅ Pattern matches!" : `❌ ${testResult.error || "Pattern does not match"}`}
          </div>
        )}

        {pattern && (
          <div className="mt-2 p-2 bg-gray-700 rounded text-xs">
            <div className="text-gray-400">Current Pattern:</div>
            <code className="text-blue-400 break-all">{pattern}</code>
          </div>
        )}
      </div>
    </div>
  )
}

function ValidationRuleEditor({ 
  rule, 
  onChange, 
  onRemove,
  canRemove 
}: { 
  rule: ValidationRuleDto
  onChange: (rule: ValidationRuleDto) => void
  onRemove: () => void
  canRemove: boolean
}) {
  const updateRule = (updates: Partial<ValidationRuleDto>) => {
    onChange({ ...rule, ...updates })
  }

  const updateParameter = (key: string, value: string | number | boolean) => {
    updateRule({
      parameters: { ...rule.parameters, [key]: value }
    })
  }

  return (
    <div className="border border-gray-600 rounded-lg p-4 bg-gray-700">
      <div className="flex justify-between items-start mb-4">
        <div className="flex-1">
          <select
            value={rule.type}
            onChange={(e) => updateRule({ type: Number(e.target.value) as ValidationRuleType, parameters: {} })}
            className="w-full px-3 py-2 bg-gray-600 border border-gray-500 rounded-lg text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
          >
            {VALIDATION_RULE_TYPES.map((type) => (
              <option key={type.value} value={type.value}>
                {type.label}
              </option>
            ))}
          </select>
        </div>
        <button
          type="button"
          onClick={onRemove}
          disabled={!canRemove}
          className={`ml-2 p-2 rounded-lg transition-colors ${
            canRemove 
              ? "text-red-400 hover:text-red-300 hover:bg-red-900" 
              : "text-gray-600 cursor-not-allowed"
          }`}
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      <div className="space-y-3">
        <div>
          <label className="block text-xs font-medium text-gray-300 mb-1">Order</label>
          <input
            type="number"
            value={rule.order}
            onChange={(e) => updateRule({ order: parseInt(e.target.value) || 0 })}
            className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            min="0"
          />
        </div>

        <div>
          <label className="block text-xs font-medium text-gray-300 mb-1">Error Message</label>
          <input
            type="text"
            value={rule.errorMessage || ""}
            onChange={(e) => updateRule({ errorMessage: e.target.value || null })}
            className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            placeholder="Optional custom error message"
          />
        </div>

        <div>
          <label className="block text-xs font-medium text-gray-300 mb-1">Parameters</label>
          {rule.type === ValidationRuleType.Required && (
            <div className="text-xs text-gray-400">No parameters required</div>
          )}

          {rule.type === ValidationRuleType.MinLength && (
            <div>
              <label className="block text-xs mb-1">Length</label>
              <input
                type="number"
                value={(rule.parameters.length as number) || 0}
                onChange={(e) => updateParameter("length", parseInt(e.target.value) || 0)}
                className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                min="1"
              />
            </div>
          )}

          {rule.type === ValidationRuleType.MaxLength && (
            <div>
              <label className="block text-xs mb-1">Length</label>
              <input
                type="number"
                value={(rule.parameters.length as number) || 0}
                onChange={(e) => updateParameter("length", parseInt(e.target.value) || 0)}
                className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                min="1"
              />
            </div>
          )}

          {rule.type === ValidationRuleType.Pattern && (
            <div>
              <label className="block text-xs mb-1">Pattern</label>
              <input
                type="text"
                value={(rule.parameters.pattern as string) || ""}
                onChange={(e) => updateParameter("pattern", e.target.value)}
                className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm font-mono text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                placeholder="Regex pattern"
              />
              <PatternTester pattern={(rule.parameters.pattern as string) || ""} />
            </div>
          )}

          {rule.type === ValidationRuleType.Range && (
            <div className="space-y-2">
              <div>
                <label className="block text-xs mb-1">Min Value</label>
                <input
                  type="number"
                  value={(rule.parameters.min as number) || 0}
                  onChange={(e) => updateParameter("min", parseFloat(e.target.value) || 0)}
                  className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>
              <div>
                <label className="block text-xs mb-1">Max Value</label>
                <input
                  type="number"
                  value={(rule.parameters.max as number) || 0}
                  onChange={(e) => updateParameter("max", parseFloat(e.target.value) || 0)}
                  className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>
            </div>
          )}

          {rule.type === ValidationRuleType.Custom && (
            <div>
              <label className="block text-xs mb-1">Custom Logic</label>
              <select
                value={(rule.parameters.customLogic as string) || ""}
                onChange={(e) => updateParameter("customLogic", e.target.value)}
                className="w-full px-2 py-1 bg-gray-600 border border-gray-500 rounded text-sm text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              >
                <option value="">Select custom logic</option>
                <option value="uppercase">Uppercase Only</option>
                <option value="lowercase">Lowercase Only</option>
                <option value="alphanumeric">Alphanumeric</option>
                <option value="numeric">Numeric Only</option>
                <option value="letters">Letters Only</option>
              </select>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
