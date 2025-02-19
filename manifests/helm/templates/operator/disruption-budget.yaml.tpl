{{ if ne .Values.operator.enabled false }}
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: contrast-agent-operator
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
spec:
  maxUnavailable: 1
  selector:
    matchLabels:
      app.kubernetes.io/name: operator
      app.kubernetes.io/part-of: contrast-agent-operator
{{ end }}
