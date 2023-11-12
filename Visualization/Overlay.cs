using UnityEngine;

namespace TerrainTools.Visualization
{
    public class Overlay
    {
        private GameObject GameObject { get; }
        private ParticleSystem.Particle[] Particles => new ParticleSystem.Particle[2];
        private Transform Transform { get; }

        public ParticleSystem ps { get; }
        public ParticleSystemRenderer psr { get; }
        public ParticleSystem.MainModule psm { get; }

        public bool Enabled
        {
            get { return GameObject.activeSelf; }
            set { GameObject.SetActive(value); }
        }

        public Vector3 Position
        {
            get { return Transform.position; }
            set { Transform.position = value; }
        }

        public Vector3 LocalPosition
        {
            get { return Transform.localPosition; }
            set { Transform.localPosition = value; }
        }

        public Quaternion Rotation
        {
            get { return Transform.rotation; }
            set { Transform.rotation = value; }
        }

        public Color Color
        {
            get { ps.GetParticles(Particles, 2); return Particles[1].GetCurrentColor(ps); }
        }

        public Color StartColor
        {
            get { return psm.startColor.color; }
            set
            {
                var psmainStartColor = psm.startColor;
                psmainStartColor.color = value;
            }
        }

        public float StartSize
        {
            get { return psm.startSize.constant; }
            set { var psMain = ps.main; psMain.startSize = value; }
        }

        public float StartSpeed
        {
            get { return psm.startSize.constant; }
            set { var psMain = ps.main; psMain.startSpeed = value; }
        }

        public float StartLifetime
        {
            get { return psm.startLifetime.constant; }
            set { var psMain = ps.main; psMain.startLifetime = value; }
        }

        public bool SizeOverLifetimeEnabled
        {
            get { return ps.sizeOverLifetime.enabled; }
            set { var psSizeOverLifetime = ps.sizeOverLifetime; psSizeOverLifetime.enabled = value; }
        }

        public ParticleSystem.MinMaxCurve SizeOverLifetime
        {
            get { return ps.sizeOverLifetime.size; }
            set { var psSizeOverLifetime = ps.sizeOverLifetime; psSizeOverLifetime.size = value; }
        }

        public Overlay(Transform transform)
        {
            this.Transform = transform;

            GameObject = transform.gameObject;
            ps = transform.GetComponentInChildren<ParticleSystem>();
            psr = transform.GetComponentInChildren<ParticleSystemRenderer>();
        }
    }
}