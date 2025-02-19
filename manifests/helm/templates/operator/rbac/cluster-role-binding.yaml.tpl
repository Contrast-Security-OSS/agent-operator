{{ if ne .Values.operator.enabled false }}
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: contrast-agent-operator-service-role-binding
  labels:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: contrast-agent-operator-service-role
subjects:
  - kind: ServiceAccount
    name: contrast-agent-operator-service-account
    namespace: '{{ default .Release.Namespace .Values.namespace }}'
{{ end }}
