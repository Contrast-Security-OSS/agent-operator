{{ if ne .Values.operator.enabled false }}
apiVersion: v1
kind: Service
metadata:
  name: contrast-agent-operator
  namespace: '{{ default .Release.Namespace .Values.namespace }}'
  labels:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
spec:
  ports:
    - name: https
      port: 443
      targetPort: https
  selector:
    app.kubernetes.io/name: operator
    app.kubernetes.io/part-of: contrast-agent-operator
{{ end }}
