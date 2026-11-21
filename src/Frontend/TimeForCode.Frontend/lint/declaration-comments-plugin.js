/**
 * oxlint plugin: a `//` comment directly above a declaration must be a `/** ... *\/` doc comment, so it shows in
 * editor tooltips. Comments inside bodies, TODO markers, `///` directives and comments separated from the
 * declaration by a blank line stay allowed. The C# counterpart is the analyzer TFC0001.
 */

const DECLARATIONS = new Set([
  'FunctionDeclaration',
  'TSDeclareFunction',
  'ClassDeclaration',
  'TSInterfaceDeclaration',
  'TSTypeAliasDeclaration',
  'TSEnumDeclaration',
  'TSEnumMember',
  'TSModuleDeclaration',
  'MethodDefinition',
  'PropertyDefinition',
  'TSAbstractMethodDefinition',
  'TSAbstractPropertyDefinition',
  'TSPropertySignature',
  'TSMethodSignature',
])

const EXPORTS = new Set(['ExportNamedDeclaration', 'ExportDefaultDeclaration'])

/** Whether the comment is the only thing on its line, so it cannot trail a statement. */
function startsItsLine(sourceCode, comment) {
  return sourceCode.lines[comment.loc.start.line - 1].slice(0, comment.loc.start.column).trim() === ''
}

function isPlainLineComment(sourceCode, comment) {
  return comment.type === 'Line' && !comment.value.startsWith('/') && startsItsLine(sourceCode, comment)
}

/** Whether the node is a declaration the rule checks. */
function isChecked(node) {
  if (EXPORTS.has(node.parent?.type)) {
    return false
  }
  if (EXPORTS.has(node.type)) {
    return node.declaration != null
  }
  if (node.type === 'VariableDeclaration') {
    return node.parent?.type === 'Program'
  }
  return DECLARATIONS.has(node.type)
}

/** Finds the first line of the `//` block that touches the node, or null when there is none or it is a TODO. */
function findLeadingBlock(sourceCode, node) {
  const comments = sourceCode.getCommentsBefore(node)
  let expectedEndLine = node.loc.start.line - 1
  let first = null

  for (let index = comments.length - 1; index >= 0; index--) {
    const comment = comments[index]
    if (comment.loc.end.line !== expectedEndLine || !isPlainLineComment(sourceCode, comment)) {
      break
    }
    first = comment
    expectedEndLine = comment.loc.start.line - 1
  }

  return first !== null && !first.value.trim().startsWith('TODO') ? first : null
}

const rule = {
  meta: {
    type: 'suggestion',
    docs: { description: 'Require a doc comment, not a line comment, above a declaration.' },
    schema: [],
    messages: {
      useDocComment: 'Use a /** ... */ doc comment instead of // above this declaration; keep // for comments inside bodies.',
    },
  },
  create(context) {
    const sourceCode = context.sourceCode
    function check(node) {
      if (!isChecked(node)) {
        return
      }
      const comment = findLeadingBlock(sourceCode, node)
      if (comment !== null) {
        context.report({ loc: comment.loc, messageId: 'useDocComment' })
      }
    }

    return Object.fromEntries([...DECLARATIONS, ...EXPORTS, 'VariableDeclaration'].map((type) => [type, check]))
  },
}

export default {
  meta: { name: 'timeforcode' },
  rules: { 'doc-comment-above-declaration': rule },
}
