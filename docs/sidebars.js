module.exports = {
  docs: [
    'docs/index',
    'docs/installation',
    {
      type: 'category',
      label: 'Getting Started',
      items: ['docs/core-api'],
      collapsed: false,
    },
  ],

  packages: [
    'packages/index',
    {
      type: 'category',
      label: 'Composed',
      items: ['packages/Composed/index'],
    },
    {
      type: 'category',
      label: 'Composed.Commands',
      items: ['packages/Composed.Commands/index'],
    },
    {
      type: 'category',
      label: 'Composed.State',
      items: ['packages/Composed.State/index'],
    },
  ],
};
